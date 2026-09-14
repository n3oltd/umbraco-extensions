export type PreviewState =
    | { status: 'loading' }
    | { status: 'ready'; markup: string }
    | { status: 'error'; message: string };

// What the coordinator needs from a block to preview it, and where to put the result.
export interface PreviewEntry {
    contentKey: string;
    // Identifies this block's current data. Two renders of the same fingerprint produce the same markup, so a
    // block whose fingerprint has not moved since it last rendered is left alone.
    fingerprint(): string;
    receive(state: PreviewState): void;
}

export interface PreviewRequestContext {
    nodeKey: string | null;
    documentTypeKey: string | null;
    // Which property holds the grid. A document type can have more than one block grid, and they are not all
    // called the same thing, so the server cannot infer it from the document alone.
    propertyAlias: string | null;
    culture: string;
}
