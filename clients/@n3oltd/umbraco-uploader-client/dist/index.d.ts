export declare class UploaderClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    getMediaById(mediaId: string | null): Promise<FileMedia>;
    protected processGetMediaById(response: Response): Promise<FileMedia>;
    upload(allowedExtensions: string | null | undefined, imagesOnly: boolean | null | undefined, maxFileSizeMb: number | null | undefined, maxHeight: number | null | undefined, maxWidth: number | null | undefined, minHeight: number | null | undefined, minWidth: number | null | undefined, file: FileParameter | null | undefined): Promise<FileMedia>;
    protected processUpload(response: Response): Promise<FileMedia>;
    getResponse(storagePath: string | null | undefined, filesizeBytes: number | undefined): Promise<FileMedia>;
    protected processGetResponse(response: Response): Promise<FileMedia>;
}
export interface FileMedia {
    urlPath?: string | undefined;
    mediaId?: string | undefined;
    filename?: string | undefined;
    extension?: string | undefined;
    sizeMb?: number;
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
}
export interface FileParameter {
    data: any;
    fileName: string;
}
export declare class ApiException extends Error {
    message: string;
    status: number;
    response: string;
    headers: {
        [key: string]: any;
    };
    result: any;
    constructor(message: string, status: number, response: string, headers: {
        [key: string]: any;
    }, result: any);
    protected isApiException: boolean;
    static isApiException(obj: any): obj is ApiException;
}
//# sourceMappingURL=index.d.ts.map