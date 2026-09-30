export declare class GoCardlessClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    beginRedirectFlow(flowId: string, req: RedirectFlowReq): Promise<PaymentFlowResOfGoCardlessCredential>;
    protected processBeginRedirectFlow(response: Response): Promise<PaymentFlowResOfGoCardlessCredential>;
    completeRedirectFlow(flowId: string): Promise<void>;
    protected processCompleteRedirectFlow(response: Response): Promise<void>;
}
export interface PaymentFlowResOfGoCardlessCredential {
    flowRevision?: number;
    result?: GoCardlessCredential | undefined;
}
export interface GoCardlessCredential {
    advancePayment?: Payment | undefined;
    setupAt?: string | undefined;
    isSetUp?: boolean;
    type?: PaymentObjectType | undefined;
    completeAt?: string | undefined;
    errorAt?: string | undefined;
    errorMessage?: string | undefined;
    exceptionDetails?: string | undefined;
    status?: PaymentObjectStatus | undefined;
    method?: string | undefined;
    clock?: IClock | undefined;
    goCardlessCustomerId?: string | undefined;
    goCardlessRedirectFlowId?: string | undefined;
    goCardlessMandateId?: string | undefined;
    goCardlessSessionToken?: string | undefined;
    goCardlessRedirectUrl?: string | undefined;
    returnUrl?: string | undefined;
}
export interface Payment {
    completeAt?: string | undefined;
    errorAt?: string | undefined;
    errorMessage?: string | undefined;
    exceptionDetails?: string | undefined;
    status?: PaymentObjectStatus | undefined;
    type?: PaymentObjectType | undefined;
    method?: string | undefined;
    clock?: IClock | undefined;
    card?: CardPayment | undefined;
    paidAt?: string | undefined;
    declinedAt?: string | undefined;
    declinedReason?: string | undefined;
    isDeclined?: boolean;
    isPaid?: boolean;
}
/** One of 'complete', 'error', 'inProgress' */
export declare enum PaymentObjectStatus {
    Complete = "complete",
    Error = "error",
    InProgress = "inProgress"
}
/** One of 'credential', 'payment' */
export declare enum PaymentObjectType {
    Credential = "credential",
    Payment = "payment"
}
/** Represents a clock which can return the current time as an Instant. */
export interface IClock {
}
export interface CardPayment {
    threeDSecureRequired?: boolean;
    threeDSecureCompleted?: boolean;
    threeDSecureV1?: ThreeDSecureV1 | undefined;
    threeDSecureV2?: ThreeDSecureV2 | undefined;
}
export interface ThreeDSecureV1 {
    acsUrl?: string | undefined;
    md?: string | undefined;
    paReq?: string | undefined;
    paRes?: string | undefined;
}
export interface ThreeDSecureV2 {
    acsUrl?: string | undefined;
    acsTransId?: string | undefined;
    sessionData?: string | undefined;
    cReq?: string | undefined;
    cRes?: string | undefined;
    html?: string | undefined;
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
}
export interface RedirectFlowReq {
    returnUrl?: string | undefined;
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