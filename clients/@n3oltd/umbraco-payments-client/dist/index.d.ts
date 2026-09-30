export declare class PaymentsClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    findPaymentMethods(req: PaymentMethodCriteria): Promise<PaymentMethodRes[]>;
    protected processFindPaymentMethods(response: Response): Promise<PaymentMethodRes[]>;
    getLookupPaymentMethods(): Promise<PaymentMethodRes[]>;
    protected processGetLookupPaymentMethods(response: Response): Promise<PaymentMethodRes[]>;
    getAllLookups(criteria: LookupsCriteria): Promise<PaymentsLookupsRes>;
    protected processGetAllLookups(response: Response): Promise<PaymentsLookupsRes>;
}
export interface PaymentMethodRes {
    name?: string | undefined;
    id?: string | undefined;
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
}
export interface PaymentMethodCriteria {
    country?: string | undefined;
    currency?: string | undefined;
}
export interface PaymentsLookupsRes {
    paymentMethods?: PaymentMethodRes[] | undefined;
}
export interface LookupsCriteria {
    types?: string[] | undefined;
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