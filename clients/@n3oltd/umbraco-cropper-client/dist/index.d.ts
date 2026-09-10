export declare class CropperClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    getMediaById(mediaId: string): Promise<ImageMedia>;
    protected processGetMediaById(response: Response): Promise<ImageMedia>;
    upload(minHeight: number | null | undefined, minWidth: number | null | undefined, file: FileParameter | null | undefined): Promise<ImageMedia>;
    protected processUpload(response: Response): Promise<ImageMedia>;
}
export interface ImageMedia {
    urlPath?: string | undefined;
    mediaId?: string | undefined;
    filename?: string | undefined;
    height?: number;
    width?: number;
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
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