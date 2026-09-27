export type PreviewState =
    | { status: 'loading' }
    | { status: 'ready'; markup: string }
    | { status: 'error'; message: string };

export interface PreviewEntry {
    contentKey: string;
    // Same fingerprint means same markup, so a block whose fingerprint has not moved is left alone.
    fingerprint(): string;
    receive(state: PreviewState): void;
}

export interface PreviewResponse {
    markup: Record<string, string>;
    failed: string[];
}

export interface PreviewRequestContext {
    nodeKey: string | null;
    documentTypeKey: string | null;
    // A document type can hold more than one block grid, so the server cannot infer this.
    propertyAlias: string | null;
    culture: string;
}
