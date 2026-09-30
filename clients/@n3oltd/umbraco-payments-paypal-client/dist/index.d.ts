export declare class PayPalClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    captureSubscription(flowId: string, req: PayPalSubscriptionReq): Promise<PaymentFlowResOfPayPalCredential>;
    protected processCaptureSubscription(response: Response): Promise<PaymentFlowResOfPayPalCredential>;
    captureTransaction(flowId: string, req: PayPalTransactionReq): Promise<PaymentFlowResOfPayPalPayment>;
    protected processCaptureTransaction(response: Response): Promise<PaymentFlowResOfPayPalPayment>;
    getOrCreatePlan(flowId: string, req: MoneyReq): Promise<string>;
    protected processGetOrCreatePlan(response: Response): Promise<string>;
}
export interface PaymentFlowResOfPayPalCredential {
    flowRevision?: number;
    result?: PayPalCredential | undefined;
}
export interface PayPalCredential {
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
    payPalErrorCode?: number | undefined;
    payPalErrorMessage?: string | undefined;
    payPalSubscriptionId?: string | undefined;
    payPalSubscriptionReason?: string | undefined;
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
export interface PayPalSubscriptionReq {
    subscriptionId?: string | undefined;
    reason?: string | undefined;
}
export interface PaymentFlowResOfPayPalPayment {
    flowRevision?: number;
    result?: PayPalPayment | undefined;
}
export interface PayPalPayment {
    card?: CardPayment | undefined;
    paidAt?: string | undefined;
    declinedAt?: string | undefined;
    declinedReason?: string | undefined;
    isDeclined?: boolean;
    isPaid?: boolean;
    type?: PaymentObjectType | undefined;
    completeAt?: string | undefined;
    errorAt?: string | undefined;
    errorMessage?: string | undefined;
    exceptionDetails?: string | undefined;
    status?: PaymentObjectStatus | undefined;
    method?: string | undefined;
    clock?: IClock | undefined;
    payPalEmail?: string | undefined;
    payPalTransactionId?: string | undefined;
}
export interface PayPalTransactionReq {
    email?: string | undefined;
    authorizationId?: string | undefined;
}
export interface MoneyReq {
    amount?: number | undefined;
    currency?: string | undefined;
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