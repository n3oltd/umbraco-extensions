export declare class CheckoutClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    getCurrentCheckout(): Promise<CheckoutRes>;
    protected processGetCurrentCheckout(response: Response): Promise<CheckoutRes>;
    getLookupCheckoutStages(): Promise<NamedLookupRes[]>;
    protected processGetLookupCheckoutStages(response: Response): Promise<NamedLookupRes[]>;
    getRegularGivingFrequencies(): Promise<NamedLookupRes[]>;
    protected processGetRegularGivingFrequencies(response: Response): Promise<NamedLookupRes[]>;
    updateAccount(checkoutRevisionId: string, req: AccountReq): Promise<CheckoutRes>;
    protected processUpdateAccount(response: Response): Promise<CheckoutRes>;
    updateAccountConsent(checkoutRevisionId: string, req: ConsentReq): Promise<CheckoutRes>;
    protected processUpdateAccountConsent(response: Response): Promise<CheckoutRes>;
    updateAccountTaxStatus(checkoutRevisionId: string, req: TaxStatusReq): Promise<CheckoutRes>;
    protected processUpdateAccountTaxStatus(response: Response): Promise<CheckoutRes>;
    updateRegularGivingOptions(checkoutRevisionId: string, req: RegularGivingOptionsReq): Promise<CheckoutRes>;
    protected processUpdateRegularGivingOptions(response: Response): Promise<CheckoutRes>;
    getAllLookups(criteria: LookupsCriteria): Promise<CheckoutLookupsRes>;
    protected processGetAllLookups(response: Response): Promise<CheckoutLookupsRes>;
}
export interface CheckoutRes {
    id?: string | undefined;
    revisionId?: string | undefined;
    reference?: Reference | undefined;
    currency?: string | undefined;
    progress?: CheckoutProgressRes | undefined;
    account?: AccountRes | undefined;
    donation?: DonationCheckoutRes | undefined;
    regularGiving?: RegularGivingCheckoutRes | undefined;
    isComplete?: boolean;
}
export interface Reference {
    type?: string | undefined;
    number?: number;
    text?: string | undefined;
}
export interface CheckoutProgressRes {
    currentStage?: string | undefined;
    requiredStages?: string[] | undefined;
    remainingStages?: string[] | undefined;
}
export interface AccountRes {
    id?: string | undefined;
    reference?: string | undefined;
    type?: AccountType | undefined;
    individual?: IndividualRes | undefined;
    organization?: OrganizationRes | undefined;
    address?: AddressRes | undefined;
    email?: EmailRes | undefined;
    telephone?: TelephoneRes | undefined;
    consent?: ConsentRes | undefined;
    taxStatus?: TaxStatus | undefined;
    token?: string | undefined;
}
/** One of 'individual', 'organization' */
export declare enum AccountType {
    Individual = "individual",
    Organization = "organization"
}
export interface IndividualRes {
    name?: NameRes | undefined;
}
export interface NameRes {
    title?: string | undefined;
    firstName?: string | undefined;
    lastName?: string | undefined;
}
export interface OrganizationRes {
    type?: OrganizationType | undefined;
    name?: string | undefined;
    contact?: NameRes | undefined;
}
/** One of 'business' */
export declare enum OrganizationType {
    Business = "business"
}
export interface AddressRes {
    line1?: string | undefined;
    line2?: string | undefined;
    line3?: string | undefined;
    locality?: string | undefined;
    administrativeArea?: string | undefined;
    postalCode?: string | undefined;
    country?: string | undefined;
}
export interface EmailRes {
    address?: string | undefined;
}
export interface TelephoneRes {
    country?: string | undefined;
    number?: string | undefined;
}
export interface ConsentRes {
    choices?: ConsentChoiceRes[] | undefined;
}
export interface ConsentChoiceRes {
    channel?: ConsentChannel | undefined;
    category?: string | undefined;
    response?: ConsentResponse | undefined;
}
/** One of 'email', 'sms', 'post', 'telephone' */
export declare enum ConsentChannel {
    Email = "email",
    Sms = "sms",
    Post = "post",
    Telephone = "telephone"
}
/** One of 'noResponse', 'optIn', 'optOut' */
export declare enum ConsentResponse {
    NoResponse = "noResponse",
    OptIn = "optIn",
    OptOut = "optOut"
}
/** One of 'payer', 'nonPayer', 'notSpecified' */
export declare enum TaxStatus {
    Payer = "payer",
    NonPayer = "nonPayer",
    NotSpecified = "notSpecified"
}
export interface DonationCheckoutRes {
    allocations?: AllocationRes[] | undefined;
    payment?: PaymentRes | undefined;
    total?: MoneyRes | undefined;
    isComplete?: boolean;
    isRequired?: boolean;
}
export interface AllocationRes {
    type?: AllocationType | undefined;
    value?: MoneyRes | undefined;
    fundDimensions?: FundDimensionValuesRes | undefined;
    feedback?: FeedbackAllocationRes | undefined;
    fund?: FundAllocationRes | undefined;
    sponsorship?: SponsorshipAllocationRes | undefined;
    upsellOfferId?: string | undefined;
    upsell?: boolean;
}
/** One of 'feedback', 'fund', 'sponsorship' */
export declare enum AllocationType {
    Feedback = "feedback",
    Fund = "fund",
    Sponsorship = "sponsorship"
}
export interface MoneyRes {
    amount?: number;
    currency?: string | undefined;
    text?: string | undefined;
}
export interface FundDimensionValuesRes {
    dimension1?: string | undefined;
    dimension2?: string | undefined;
    dimension3?: string | undefined;
    dimension4?: string | undefined;
}
export interface FeedbackAllocationRes {
    scheme?: string | undefined;
    customFields?: FeedbackCustomFieldRes[] | undefined;
}
/** One of 'donation', 'regularGiving' */
export declare enum GivingType {
    Donation = "donation",
    RegularGiving = "regularGiving"
}
export interface FeedbackCustomFieldDefinitionElement {
    type?: FeedbackCustomFieldType | undefined;
    name?: string | undefined;
    required?: boolean;
    textMaxLength?: number | undefined;
    alias?: string | undefined;
}
/** One of 'bool', 'date', 'text' */
export declare enum FeedbackCustomFieldType {
    Bool = "bool",
    Date = "date",
    Text = "text"
}
export interface PriceContent {
    amount?: number;
    locked?: boolean;
}
export interface PricingRuleElement {
    amount?: number;
    locked?: boolean;
    dimension1?: string | undefined;
    dimension2?: string | undefined;
    dimension3?: string | undefined;
    dimension4?: string | undefined;
}
export interface FeedbackCustomFieldRes {
    type?: FeedbackCustomFieldType | undefined;
    alias?: string | undefined;
    name?: string | undefined;
    bool?: boolean | undefined;
    date?: string | undefined;
    text?: string | undefined;
}
export interface FundAllocationRes {
    donationItem?: string | undefined;
}
export interface SponsorshipAllocationRes {
    beneficiaryReference?: string | undefined;
    scheme?: string | undefined;
    duration?: SponsorshipDuration | undefined;
    components?: SponsorshipComponentAllocationRes[] | undefined;
}
/** One of '_6', '_12', '_18', '_24', '_36', '_48', '_60' */
export declare enum SponsorshipDuration {
    _6 = "_6",
    _12 = "_12",
    _18 = "_18",
    _24 = "_24",
    _36 = "_36",
    _48 = "_48",
    _60 = "_60"
}
export interface SponsorshipComponentAllocationRes {
    component?: string | undefined;
    value?: MoneyRes | undefined;
}
export interface PaymentRes {
    type?: PaymentObjectType | undefined;
    method?: string | undefined;
    status?: PaymentObjectStatus | undefined;
    errorMessage?: string | undefined;
    hasError?: boolean;
    isComplete?: boolean;
    isInProgress?: boolean;
    card?: CardPaymentRes | undefined;
    declinedReason?: string | undefined;
    isDeclined?: boolean;
    isPaid?: boolean;
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
export interface CardPaymentRes {
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
export interface RegularGivingCheckoutRes {
    allocations?: AllocationRes[] | undefined;
    credential?: CredentialRes | undefined;
    options?: RegularGivingOptionsRes | undefined;
    total?: MoneyRes | undefined;
    isComplete?: boolean;
    isRequired?: boolean;
}
export interface CredentialRes {
    type?: PaymentObjectType | undefined;
    method?: string | undefined;
    status?: PaymentObjectStatus | undefined;
    errorMessage?: string | undefined;
    hasError?: boolean;
    isComplete?: boolean;
    isInProgress?: boolean;
    advancePayment?: PaymentRes | undefined;
    setupAt?: string | undefined;
    isSetUp?: boolean;
}
export interface RegularGivingOptionsRes {
    preferredCollectionDay?: string | undefined;
    frequency?: RegularGivingFrequency | undefined;
    firstCollectionDate?: string | undefined;
}
/** One of 'annually', 'monthly', 'quarterly' */
export declare enum RegularGivingFrequency {
    Annually = "annually",
    Monthly = "monthly",
    Quarterly = "quarterly"
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
}
export interface NamedLookupRes {
    id?: string | undefined;
    name?: string | undefined;
}
export interface AccountReq {
    id?: string | undefined;
    reference?: string | undefined;
    type?: AccountType | undefined;
    individual?: IndividualReq | undefined;
    organization?: OrganizationReq | undefined;
    address?: AddressReq | undefined;
    email?: EmailReq | undefined;
    telephone?: TelephoneReq | undefined;
    consent?: ConsentReq | undefined;
    taxStatus?: TaxStatus | undefined;
    captcha?: CaptchaReq | undefined;
}
export interface IndividualReq {
    name?: NameReq | undefined;
}
export interface NameReq {
    title?: string | undefined;
    firstName?: string | undefined;
    lastName?: string | undefined;
}
export interface OrganizationReq {
    type?: OrganizationType | undefined;
    name?: string | undefined;
    contact?: NameReq | undefined;
}
export interface AddressReq {
    line1?: string | undefined;
    line2?: string | undefined;
    line3?: string | undefined;
    locality?: string | undefined;
    administrativeArea?: string | undefined;
    postalCode?: string | undefined;
    country?: string | undefined;
}
export interface EmailReq {
    address?: string | undefined;
}
export interface TelephoneReq {
    country?: string | undefined;
    number?: string | undefined;
}
export interface ConsentReq {
    choices?: ConsentChoiceReq[] | undefined;
}
export interface ConsentChoiceReq {
    channel?: ConsentChannel | undefined;
    category?: string | undefined;
    response?: ConsentResponse | undefined;
    categoryName?: string | undefined;
}
export interface CaptchaReq {
    token?: string | undefined;
    action?: string | undefined;
}
export interface TaxStatusReq {
    taxStatus?: TaxStatus | undefined;
}
export interface RegularGivingOptionsReq {
    preferredCollectionDay?: string | undefined;
    frequency?: RegularGivingFrequency | undefined;
    firstCollectionDate?: string | undefined;
}
export interface CheckoutLookupsRes {
    checkoutStages?: NamedLookupRes[] | undefined;
    regularGivingFrequencies?: NamedLookupRes[] | undefined;
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