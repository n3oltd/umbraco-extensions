import type { UmbBlockManagerContext } from '@umbraco-cms/backoffice/block';
import type { AuthFetch } from '@n3oltd/backoffice-core';
import type { PreviewEntry, PreviewRequestContext } from './types';

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
        this.#authFetch = authFetch;
    }

    setContext(context: PreviewRequestContext): void {
        const changed = context.nodeKey !== this.#context.nodeKey ||
                        context.documentTypeKey !== this.#context.documentTypeKey ||
                        context.propertyAlias !== this.#context.propertyAlias ||
                        context.culture !== this.#context.culture;

        this.#context = context;

        // The node and culture are what the markup is rendered against, so changing either invalidates every
        // block, not just the ones being edited.
        if (changed) {
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
        if (this.#inFlight) {
            this.#flushAgain = true;

            return;
        }

        const due = [...this.#pending]
            .map((key) => this.#entries.get(key))
            .filter((entry): entry is PreviewEntry => !!entry)
            .filter((entry) => this.#rendered.get(entry.contentKey) !== entry.fingerprint());

        this.#pending.clear();

        if (!due.length || !this.#authFetch) {
            return;
        }

        const blockValue = this.#buildBlockValue();

        if (!blockValue) {
            return;
        }

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

            const markup: Record<string, string> = await response.json();

            for (const entry of due) {
                const blockMarkup = markup[entry.contentKey];

                if (typeof blockMarkup === 'string') {
                    this.#rendered.set(entry.contentKey, fingerprints.get(entry.contentKey)!);
                    entry.receive({ status: 'ready', markup: blockMarkup });
                } else {
                    entry.receive({ status: 'error', message: previewFailedMessage });
                }
            }
        } catch (error) {
            if (!abort.signal.aborted) {
                console.error('Block preview failed', error);

                for (const entry of due) {
                    this.#rendered.delete(entry.contentKey);
                    entry.receive({ status: 'error', message: previewFailedMessage });
                }
            }
        } finally {
            this.#inFlight = undefined;

            if (this.#flushAgain) {
                this.#flushAgain = false;
                this.#schedule(0);
            }
        }
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
