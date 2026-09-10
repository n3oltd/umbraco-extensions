export declare class MarketingExportClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    getDaily(siteId: string | null | undefined, from: string | null | undefined, to: string | null | undefined): Promise<DailyRes>;
    protected processGetDaily(response: Response): Promise<DailyRes>;
    getSites(): Promise<SitesRes>;
    protected processGetSites(response: Response): Promise<SitesRes>;
    getSite(siteId: string): Promise<SiteRes>;
    protected processGetSite(response: Response): Promise<SiteRes>;
}
export interface DailyRes {
    goals?: GoalRow[] | undefined;
    traffic?: TrafficRow[] | undefined;
}
export interface GoalRow {
    campaign?: string | undefined;
    count?: number;
    date?: string | undefined;
    medium?: string | undefined;
    name?: string | undefined;
    referrer?: string | undefined;
    source?: string | undefined;
    value?: number;
}
export interface TrafficRow {
    campaign?: string | undefined;
    date?: string | undefined;
    medium?: string | undefined;
    newUsers?: number;
    pageviews?: number;
    referrer?: string | undefined;
    sessions?: number;
    source?: string | undefined;
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
}
export interface SitesRes {
    sites?: SiteRes[] | undefined;
}
export interface SiteRes {
    currencyCode?: string | undefined;
    id?: string | undefined;
    name?: string | undefined;
    timeZone?: string | undefined;
    url?: string | undefined;
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