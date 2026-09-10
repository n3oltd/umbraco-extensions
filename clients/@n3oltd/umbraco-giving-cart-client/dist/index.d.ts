export declare class CartClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    add(req: AddToCartReq): Promise<void>;
    protected processAdd(response: Response): Promise<void>;
    addUpsellToCart(upsellOfferId: string, req: AddUpsellToCartReq): Promise<void>;
    protected processAddUpsellToCart(response: Response): Promise<void>;
    bulkRemove(req: BulkRemoveFromCartReq): Promise<void>;
    protected processBulkRemove(response: Response): Promise<void>;
    getSummary(): Promise<CartSummaryRes>;
    protected processGetSummary(response: Response): Promise<CartSummaryRes>;
    reset(): Promise<void>;
    protected processReset(response: Response): Promise<void>;
    remove(req: RemoveFromCartReq): Promise<void>;
    protected processRemove(response: Response): Promise<void>;
    removeUpsellFromCart(upsellOfferId: string): Promise<void>;
    protected processRemoveUpsellFromCart(response: Response): Promise<void>;
}
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
}
export interface AddToCartReq {
    givingType?: GivingType | undefined;
    allocation?: AllocationReq | undefined;
    quantity?: number | undefined;
}
/** One of 'donation', 'regularGiving' */
export declare enum GivingType {
    Donation = "donation",
    RegularGiving = "regularGiving"
}
export interface AllocationReq {
    type?: AllocationType | undefined;
    value?: MoneyReq | undefined;
    fundDimensions?: FundDimensionValuesReq | undefined;
    feedback?: FeedbackAllocationReq | undefined;
    fund?: FundAllocationReq | undefined;
    sponsorship?: SponsorshipAllocationReq | undefined;
    pledgeUrl?: string | undefined;
    upsellOfferId?: string | undefined;
    notes?: string | undefined;
    [key: string]: any;
}
/** One of 'feedback', 'fund', 'sponsorship' */
export declare enum AllocationType {
    Feedback = "feedback",
    Fund = "fund",
    Sponsorship = "sponsorship"
}
export interface MoneyReq {
    amount?: number | undefined;
    currency?: string | undefined;
}
export interface FundDimensionValuesReq {
    dimension1?: string | undefined;
    dimension2?: string | undefined;
    dimension3?: string | undefined;
    dimension4?: string | undefined;
}
export interface FeedbackAllocationReq {
    scheme?: string | undefined;
    customFields?: FeedbackNewCustomFieldsReq | undefined;
}
export interface FundDimensionOptions {
    dimension1?: string[] | undefined;
    dimension2?: string[] | undefined;
    dimension3?: string[] | undefined;
    dimension4?: string[] | undefined;
}
export interface Pricing {
    price?: Price | undefined;
    rules?: PricingRule[] | undefined;
}
export interface Price {
    amount?: number;
    locked?: boolean;
}
export interface PricingRule {
    price?: Price | undefined;
    fundDimensions?: FundDimensionValues | undefined;
}
export interface FundDimensionValues {
    dimension1?: string | undefined;
    dimension2?: string | undefined;
    dimension3?: string | undefined;
    dimension4?: string | undefined;
}
export interface FeedbackCustomFieldDefinition {
    type?: FeedbackCustomFieldType | undefined;
    alias?: string | undefined;
    name?: string | undefined;
    required?: boolean;
    textMaxLength?: number | undefined;
}
/** One of 'bool', 'date', 'text' */
export declare enum FeedbackCustomFieldType {
    Bool = "bool",
    Date = "date",
    Text = "text"
}
export interface FeedbackNewCustomFieldsReq {
    entries?: FeedbackNewCustomFieldReq[] | undefined;
}
export interface FeedbackNewCustomFieldReq {
    alias?: string | undefined;
    bool?: boolean | undefined;
    date?: string | undefined;
    text?: string | undefined;
}
export interface FundAllocationReq {
    donationItem?: string | undefined;
}
export interface SponsorshipAllocationReq {
    beneficiaryReference?: string | undefined;
    scheme?: string | undefined;
    duration?: SponsorshipDuration | undefined;
    components?: SponsorshipComponentAllocationReq[] | undefined;
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
export interface SponsorshipComponentAllocationReq {
    component?: string | undefined;
    value?: MoneyReq | undefined;
}
export interface AddUpsellToCartReq {
    amount?: number | undefined;
}
export interface BulkRemoveFromCartReq {
    givingType?: GivingType | undefined;
    indexes?: number[] | undefined;
}
export interface CartSummaryRes {
    revisionId?: string | undefined;
    itemCount?: number;
}
export interface RemoveFromCartReq {
    givingType?: GivingType | undefined;
    index?: number | undefined;
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