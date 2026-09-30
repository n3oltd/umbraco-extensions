export declare class BamboraClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    chargeCard(flowId: string, req: ChargeCardReq): Promise<PaymentFlowResOfBamboraPayment>;
    protected processChargeCard(response: Response): Promise<PaymentFlowResOfBamboraPayment>;
    completePaymentThreeDSecure(flowId: string, cRes: string | null | undefined, paRes: string | null | undefined): Promise<void>;
    protected processCompletePaymentThreeDSecure(response: Response): Promise<void>;
    storeCard(flowId: string, req: StoreCardReq): Promise<PaymentFlowResOfBamboraCredential>;
    protected processStoreCard(response: Response): Promise<PaymentFlowResOfBamboraCredential>;
}
export interface PaymentFlowResOfBamboraPayment {
    flowRevision?: number;
    result?: BamboraPayment | undefined;
}
export interface BamboraPayment {
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
    bamboraErrorCode?: number | undefined;
    bamboraErrorMessage?: string | undefined;
    bamboraPaymentId?: string | undefined;
    bamboraStatusCode?: number | undefined;
    bamboraStatusDetail?: string | undefined;
    bamboraToken?: string | undefined;
    returnUrl?: string | undefined;
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
export interface ChargeCardReq {
    token?: string | undefined;
    returnUrl?: string | undefined;
    browserParameters?: BrowserParametersReq | undefined;
}
export interface BrowserParametersReq {
    colourDepth?: number | undefined;
    javaEnabled?: boolean | undefined;
    javaScriptEnabled?: boolean | undefined;
    screenHeight?: number | undefined;
    screenWidth?: number | undefined;
    utcOffsetMinutes?: number | undefined;
}
export interface PaymentFlowResOfBamboraCredential {
    flowRevision?: number;
    result?: BamboraCredential | undefined;
}
export interface BamboraCredential {
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
    bamboraErrorCode?: number | undefined;
    bamboraErrorMessage?: string | undefined;
    bamboraCustomerCode?: string | undefined;
    bamboraStatusCode?: number | undefined;
    bamboraStatusDetail?: string | undefined;
    bamboraToken?: string | undefined;
    returnUrl?: string | undefined;
    cardPayment?: CardPayment | undefined;
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
export interface StoreCardReq {
    token?: string | undefined;
    returnUrl?: string | undefined;
    browserParameters?: BrowserParametersReq | undefined;
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