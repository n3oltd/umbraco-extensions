export type PreviewState =
    | { status: 'loading' }
    | { status: 'ready'; markup: string }
    | { status: 'error'; message: string };

export interface PreviewEntry {
    contentKey: string;
    // Must change whenever the block's markup could, or the block keeps a stale preview.
    fingerprint(): string;
    receive(state: PreviewState): void;
}

export interface PreviewResponse {
    markup: Record<string, string>;
}

export interface PreviewRequestContext {
    nodeKey: string | null;
    documentTypeKey: string | null;
    propertyAlias: string | null;
    culture: string;
}
