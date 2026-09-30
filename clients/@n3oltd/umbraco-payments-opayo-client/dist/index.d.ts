export declare class OpayoClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    chargeCard(flowId: string, req: ChargeCardReq): Promise<PaymentFlowResOfOpayoPayment>;
    protected processChargeCard(response: Response): Promise<PaymentFlowResOfOpayoPayment>;
    completeThreeDSecureChallenge(flowId: string, cRes: string | null | undefined, paRes: string | null | undefined): Promise<void>;
    protected processCompleteThreeDSecureChallenge(response: Response): Promise<void>;
    getApplePaySession(): Promise<ApplePaySessionRes>;
    protected processGetApplePaySession(response: Response): Promise<ApplePaySessionRes>;
    getMerchantSessionKey(): Promise<MerchantSessionKeyRes>;
    protected processGetMerchantSessionKey(response: Response): Promise<MerchantSessionKeyRes>;
    storeCard(flowId: string, req: StoreCardReq): Promise<PaymentFlowResOfOpayoCredential>;
    protected processStoreCard(response: Response): Promise<PaymentFlowResOfOpayoCredential>;
}
export interface PaymentFlowResOfOpayoPayment {
    flowRevision?: number;
    result?: OpayoPayment | undefined;
}
export interface OpayoPayment {
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
    opayoTransactionId?: string | undefined;
    opayoStatusCode?: number | undefined;
    opayoStatusDetail?: string | undefined;
    opayoMerchantSessionKey?: string | undefined;
    opayoErrorCode?: number | undefined;
    opayoErrorMessage?: string | undefined;
    opayoBankAuthorisationCode?: string | undefined;
    opayoRetrievalReference?: number | undefined;
    returnUrl?: string | undefined;
    vendorTxCode?: string | undefined;
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
    merchantSessionKey?: string | undefined;
    cardIdentifier?: string | undefined;
    googlePayToken?: string | undefined;
    applePayToken?: ApplePayTokenReq | undefined;
    browserParameters?: BrowserParametersReq | undefined;
    challengeWindowSize?: ChallengeWindowSize | undefined;
    returnUrl?: string | undefined;
}
export interface ApplePayTokenReq {
    applicationData?: string | undefined;
    displayName?: string | undefined;
    paymentData?: string | undefined;
    sessionValidationToken?: string | undefined;
}
export interface BrowserParametersReq {
    colourDepth?: number | undefined;
    javaEnabled?: boolean | undefined;
    javaScriptEnabled?: boolean | undefined;
    screenHeight?: number | undefined;
    screenWidth?: number | undefined;
    utcOffsetMinutes?: number | undefined;
}
/** One of 'small', 'medium', 'large', 'extraLarge', 'fullScreen' */
export declare enum ChallengeWindowSize {
    Small = "small",
    Medium = "medium",
    Large = "large",
    ExtraLarge = "extraLarge",
    FullScreen = "fullScreen"
}
export interface ApplePaySessionRes {
    status?: string | undefined;
    statusCode?: string | undefined;
    statusDetail?: string | undefined;
    epochTimeStamp?: number;
    expiresAt?: number;
    merchantSessionIdentifier?: string | undefined;
    nonce?: string | undefined;
    merchantIdentifier?: string | undefined;
    domainName?: string | undefined;
    displayName?: string | undefined;
    signature?: string | undefined;
    sessionValidationToken?: string | undefined;
}
export interface MerchantSessionKeyRes {
    key?: string | undefined;
    expiresAt?: string;
}
export interface PaymentFlowResOfOpayoCredential {
    flowRevision?: number;
    result?: OpayoCredential | undefined;
}
export interface OpayoCredential {
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
    advancePayment?: ChargeCardReq | undefined;
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