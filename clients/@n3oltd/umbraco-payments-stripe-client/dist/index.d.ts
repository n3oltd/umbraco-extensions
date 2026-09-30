export declare class StripeClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    createPaymentIntent(flowId: string, req: PaymentIntentReq): Promise<PaymentFlowResOfStripePayment>;
    protected processCreatePaymentIntent(response: Response): Promise<PaymentFlowResOfStripePayment>;
    createSetupIntent(flowId: string, req: SetupIntentReq): Promise<PaymentFlowResOfStripeCredential>;
    protected processCreateSetupIntent(response: Response): Promise<PaymentFlowResOfStripeCredential>;
    confirmPaymentIntent(flowId: string): Promise<PaymentFlowResOfStripePayment>;
    protected processConfirmPaymentIntent(response: Response): Promise<PaymentFlowResOfStripePayment>;
    confirmSetupIntent(flowId: string): Promise<PaymentFlowResOfStripeCredential>;
    protected processConfirmSetupIntent(response: Response): Promise<PaymentFlowResOfStripeCredential>;
}
export interface PaymentFlowResOfStripePayment {
    flowRevision?: number;
    result?: StripePayment | undefined;
}
export interface StripePayment {
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
    stripeChargeId?: string | undefined;
    stripeCustomerId?: string | undefined;
    stripeDeclineCode?: string | undefined;
    stripeErrorCode?: string | undefined;
    stripeErrorMessage?: string | undefined;
    stripePaymentIntentId?: string | undefined;
    stripePaymentIntentClientSecret?: string | undefined;
    stripePaymentMethodId?: string | undefined;
    actionRequired?: boolean;
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
/** One of 'credential', 'payment' */
export declare enum PaymentObjectType {
    Credential = "credential",
    Payment = "payment"
}
/** One of 'complete', 'error', 'inProgress' */
export declare enum PaymentObjectStatus {
    Complete = "complete",
    Error = "error",
    InProgress = "inProgress"
}
/** Represents a clock which can return the current time as an Instant. */
export interface IClock {
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
}
export interface PaymentIntentReq {
    paymentMethodId?: string | undefined;
}
export interface PaymentFlowResOfStripeCredential {
    flowRevision?: number;
    result?: StripeCredential | undefined;
}
export interface StripeCredential {
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
    stripeMandateId?: string | undefined;
    stripeCustomerId?: string | undefined;
    stripeDeclineCode?: string | undefined;
    stripeErrorCode?: string | undefined;
    stripeErrorMessage?: string | undefined;
    stripeSetupIntentId?: string | undefined;
    stripeSetupIntentClientSecret?: string | undefined;
    stripePaymentMethodId?: string | undefined;
    actionRequired?: boolean;
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
export interface SetupIntentReq {
    paymentMethodId?: string | undefined;
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