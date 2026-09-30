export declare class ContentClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    findChildren(contentId: string, req: ContentCriteria): Promise<ContentRes[]>;
    protected processFindChildren(response: Response): Promise<ContentRes[]>;
    findDescendants(contentId: string, req: ContentCriteria): Promise<ContentRes[]>;
    protected processFindDescendants(response: Response): Promise<ContentRes[]>;
    getById(contentId: string): Promise<ContentRes>;
    protected processGetById(response: Response): Promise<ContentRes>;
}
export interface ContentRes {
    id?: number;
    key?: string;
    url?: string | undefined;
    level?: number;
    createDate?: Date;
    updateDate?: Date;
    creatorName?: string | undefined;
    writerName?: string | undefined;
    name?: string | undefined;
    parentId?: number | undefined;
    sortOrder?: number;
    contentTypeAlias?: string | undefined;
    properties?: {
        [key: string]: any;
    } | undefined;
    [key: string]: any;
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
}
export interface ContentCriteria {
    contentTypeAlias?: string | undefined;
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