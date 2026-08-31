import type { UmbBlockManagerContext } from '@umbraco-cms/backoffice/block';
import type { AuthFetch } from '@n3oltd/backoffice-core';
import type { PreviewEntry, PreviewRequestContext, PreviewResponse } from './types';

const previewEndpoint = '/umbraco/backoffice/api/blockPreviewBackoffice/previewGridBlocks';
const editDebounceMs = 500;

export const previewFailedMessage = 'Failed getting block preview markup';

// Every block in one grid previews from that grid's whole value, so a per-block request sends the same payload
// as many times as there are blocks and makes the server convert it again each time. One coordinator per block
// manager collects the blocks that actually need markup and asks for all of them together.
export class PreviewCoordinator {
    readonly #blockManager: UmbBlockManagerContext;
    readonly #entries = new Map<string, PreviewEntry>();
    readonly #pending = new Set<string>();
    readonly #rendered = new Map<string, string>();

    #context: PreviewRequestContext = { nodeKey: null, documentTypeKey: null, propertyAlias: null, culture: '' };
    #authFetch: AuthFetch | null = null;
    #flushHandle: ReturnType<typeof setTimeout> | undefined;
    #inFlight: AbortController | undefined;
    #flushAgain = false;

    constructor(blockManager: UmbBlockManagerContext) {
        this.#blockManager = blockManager;
    }

    register(entry: PreviewEntry): void {
        const existing = this.#entries.get(entry.contentKey);

        // Moving a block between areas tears its element down and builds a new one, and the old element
        // unregisters after the new one has registered. What was remembered about the key describes markup
        // the element that has gone was showing, so the new one must not be treated as already rendered.
        if (existing && existing !== entry) {
            this.#rendered.delete(entry.contentKey);
        }

        this.#entries.set(entry.contentKey, entry);
    }

    unregister(entry: PreviewEntry): void {
        if (this.#entries.get(entry.contentKey) === entry) {
            this.#entries.delete(entry.contentKey);
            this.#pending.delete(entry.contentKey);
            this.#rendered.delete(entry.contentKey);
        }
    }

    setAuthFetch(authFetch: AuthFetch | null): void {
        // Every block pushes its own auth context into this one shared field, and the mixin reports null both
        // before the context resolves and when an element disconnects. A block being dragged or deleted must
        // not take the token away from the blocks that remain.
        if (authFetch) {
            this.#authFetch = authFetch;
        }
    }

    setContext(context: PreviewRequestContext): void {
        const changed = context.nodeKey !== this.#context.nodeKey ||
                        context.documentTypeKey !== this.#context.documentTypeKey ||
                        context.propertyAlias !== this.#context.propertyAlias ||
                        context.culture !== this.#context.culture;

        this.#context = context;

        // The markup is rendered against the whole context, so any change invalidates every block. Whatever is
        // in flight was rendered against the old context, so its reply must not be applied.
        if (changed) {
            this.#inFlight?.abort();
            this.#rendered.clear();

            for (const key of this.#entries.keys()) {
                this.#pending.add(key);
            }

            this.#schedule(0);
        }
    }

    // delay is 0 for the reasons a block appears (mounted, scrolled into view) and debounced for editing, where
    // every keystroke would otherwise be a render.
    request(entry: PreviewEntry, delay = editDebounceMs): void {
        this.#pending.add(entry.contentKey);
        this.#schedule(delay);
    }

    #schedule(delay: number): void {
        clearTimeout(this.#flushHandle);

        this.#flushHandle = setTimeout(() => { void this.#flush(); }, delay);
    }

    async #flush(): Promise<void> {
        // Batches are sent one at a time and a flush arriving during one is folded into the next, rather than
        // superseding it: the blocks already sent have been taken out of the queue, so cancelling their request
        // would leave them waiting for a reply that never comes.
        if (this.#inFlight) {
            this.#flushAgain = true;

            return;
        }

        const due = [...this.#pending]
            .map((key) => this.#entries.get(key))
            .filter((entry): entry is PreviewEntry => !!entry)
            .filter((entry) => this.#rendered.get(entry.contentKey) !== entry.fingerprint());

        if (!due.length || !this.#authFetch) {
            return;
        }

        const blockValue = this.#buildBlockValue();

        if (!blockValue) {
            return;
        }

        // Cleared only once the request is going out, so a flush arriving before the auth token resolves or
        // before the block manager has layouts leaves the blocks queued instead of dropping them.
        this.#pending.clear();

        // Captured before the request so a block edited while it is in flight is not recorded as rendered at
        // its new fingerprint.
        const fingerprints = new Map(due.map((entry) => [entry.contentKey, entry.fingerprint()]));

        for (const entry of due) {
            if (!this.#rendered.has(entry.contentKey)) {
                entry.receive({ status: 'loading' });
            }
        }

        const abort = new AbortController();
        this.#inFlight = abort;

        try {
            const response = await this.#authFetch(this.#buildUrl(), {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                body: JSON.stringify({ blockKeys: due.map((x) => x.contentKey), blockValue }),
                signal: abort.signal,
            });

            if (!response.ok) {
                throw new Error(`Preview request failed with status ${response.status}`);
            }

            const preview: PreviewResponse = await response.json();
            const failed = new Set(preview.failed ?? []);

            for (const entry of due) {
                const blockMarkup = preview.markup?.[entry.contentKey];

                if (typeof blockMarkup !== 'string') {
                    this.#retry(entry);

                    continue;
                }

                // A block the server could not render is answered with banner markup, which is why it has to
                // say which those were. Recording one as rendered would pin the banner until the block is
                // edited or the document reloaded.
                if (failed.has(entry.contentKey)) {
                    this.#rendered.delete(entry.contentKey);
                    this.#pending.add(entry.contentKey);
                } else {
                    this.#rendered.set(entry.contentKey, fingerprints.get(entry.contentKey)!);
                }

                entry.receive({ status: 'ready', markup: blockMarkup });
            }
        } catch (error) {
            if (abort.signal.aborted) {
                // Cancelled rather than failed, so the blocks are queued again for the flush that cancelled it.
                for (const entry of due) {
                    this.#pending.add(entry.contentKey);
                }
            } else {
                console.error('Block preview failed', error);

                for (const entry of due) {
                    this.#retry(entry);
                }
            }
        } finally {
            if (this.#inFlight === abort) {
                this.#inFlight = undefined;
            }

            if (this.#flushAgain) {
                this.#flushAgain = false;

                this.#schedule(0);
            }
        }
    }

    // Queued again but deliberately not rescheduled, so the next edit, scroll or context change tries again
    // rather than this spinning against a server that is failing.
    #retry(entry: PreviewEntry): void {
        this.#rendered.delete(entry.contentKey);
        this.#pending.add(entry.contentKey);

        entry.receive({ status: 'error', message: previewFailedMessage });
    }

    #buildBlockValue() {
        const layouts = this.#blockManager.getLayouts();

        if (!layouts) {
            return null;
        }

        return {
            layout: { 'Umbraco.BlockGrid': layouts },
            contentData: this.#blockManager.getContents(),
            settingsData: this.#blockManager.getSettings(),
            expose: this.#blockManager.getExposes(),
        };
    }

    #buildUrl(): string {
        const query = new URLSearchParams({
            nodeKey: this.#context.nodeKey ?? '',
            documentTypeKey: this.#context.documentTypeKey ?? '',
            propertyAlias: this.#context.propertyAlias ?? '',
            culture: this.#context.culture,
        });

        return `${previewEndpoint}?${query}`;
    }
}

const coordinators = new WeakMap<UmbBlockManagerContext, PreviewCoordinator>();

export function coordinatorFor(blockManager: UmbBlockManagerContext): PreviewCoordinator {
    let coordinator = coordinators.get(blockManager);

    if (!coordinator) {
        coordinator = new PreviewCoordinator(blockManager);
        coordinators.set(blockManager, coordinator);
    }

    return coordinator;
}
