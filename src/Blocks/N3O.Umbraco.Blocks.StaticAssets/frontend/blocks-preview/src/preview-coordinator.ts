import type { UmbBlockManagerContext } from '@umbraco-cms/backoffice/block';
import type { AuthFetch } from '@n3oltd/backoffice-core';
import type { PreviewEntry, PreviewRequestContext, PreviewResponse } from './types';

const previewEndpoint = '/umbraco/backoffice/api/blockPreviewBackoffice/previewGridBlocks';
const editDebounceMs = 500;

export const previewFailedMessage = 'Failed getting block preview markup';

interface Answer {
    entry: PreviewEntry;
    fingerprint: string;
}

export class PreviewCoordinator {
    readonly #blockManager: UmbBlockManagerContext;
    readonly #entries = new Map<string, PreviewEntry>();
    readonly #pending = new Set<string>();
    readonly #answers = new Map<string, Answer>();

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
        }
    }

    setAuthFetch(authFetch: AuthFetch | null): void {
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

        if (changed) {
            this.#inFlight?.abort();
            this.#answers.clear();

            for (const key of this.#entries.keys()) {
                this.#pending.add(key);
            }

            this.#schedule(0);
        }
    }

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
            .filter((entry) => !this.#isAnswered(entry));

        if (!due.length || !this.#authFetch || !this.#context.propertyAlias) {
            return;
        }

        this.#pending.clear();

        const fingerprints = new Map(due.map((entry) => [entry.contentKey, entry.fingerprint()]));
        const abort = new AbortController();
        this.#inFlight = abort;

        try {
            const response = await this.#authFetch(this.#buildUrl(), {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                body: JSON.stringify({ blockKeys: due.map((x) => x.contentKey), blockValue: this.#buildBlockValue() }),
                signal: abort.signal,
            });

            if (!response.ok) {
                throw new Error(`Preview request failed with status ${response.status}`);
            }

            const preview: PreviewResponse = await response.json();

            for (const entry of due) {
                const blockMarkup = preview.markup[entry.contentKey];

                if (typeof blockMarkup !== 'string') {
                    this.#fail(entry);

                    continue;
                }

                this.#answers.set(entry.contentKey, { entry, fingerprint: fingerprints.get(entry.contentKey)! });

                entry.receive({ status: 'ready', markup: blockMarkup });
            }
        } catch (error) {
            if (abort.signal.aborted) {
                for (const entry of due) {
                    this.#pending.add(entry.contentKey);
                }
            } else {
                console.error('Block preview failed', error);

                for (const entry of due) {
                    this.#fail(entry);
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

    // Not rescheduled: retrying from here would resend a failing request in a loop.
    #fail(entry: PreviewEntry): void {
        this.#pending.add(entry.contentKey);

        entry.receive({ status: 'error', message: previewFailedMessage });
    }

    #isAnswered(entry: PreviewEntry): boolean {
        const answer = this.#answers.get(entry.contentKey);

        return answer?.entry === entry && answer.fingerprint === entry.fingerprint();
    }

    #buildBlockValue() {
        return {
            layout: { 'Umbraco.BlockGrid': this.#blockManager.getLayouts() },
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
