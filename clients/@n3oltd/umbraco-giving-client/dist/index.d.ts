export declare class GivingClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    getDonationForm(donationFormId: string): Promise<DonationFormRes>;
    protected processGetDonationForm(response: Response): Promise<DonationFormRes>;
    getFundStructure(): Promise<FundStructureRes>;
    protected processGetFundStructure(response: Response): Promise<FundStructureRes>;
    getLookupAllocationTypes(): Promise<NamedLookupRes[]>;
    protected processGetLookupAllocationTypes(response: Response): Promise<NamedLookupRes[]>;
    getLookupCurrencies(): Promise<CurrencyRes[]>;
    protected processGetLookupCurrencies(response: Response): Promise<CurrencyRes[]>;
    getLookupDonationItems(): Promise<DonationItemRes[]>;
    protected processGetLookupDonationItems(response: Response): Promise<DonationItemRes[]>;
    getLookupFeedbackSchemes(): Promise<FeedbackSchemeRes[]>;
    protected processGetLookupFeedbackSchemes(response: Response): Promise<FeedbackSchemeRes[]>;
    getLookupFundDimension1Values(): Promise<FundDimensionValueRes[]>;
    protected processGetLookupFundDimension1Values(response: Response): Promise<FundDimensionValueRes[]>;
    getLookupFundDimension2Values(): Promise<FundDimensionValueRes[]>;
    protected processGetLookupFundDimension2Values(response: Response): Promise<FundDimensionValueRes[]>;
    getLookupFundDimension3Values(): Promise<FundDimensionValueRes[]>;
    protected processGetLookupFundDimension3Values(response: Response): Promise<FundDimensionValueRes[]>;
    getLookupFundDimension4Values(): Promise<FundDimensionValueRes[]>;
    protected processGetLookupFundDimension4Values(response: Response): Promise<FundDimensionValueRes[]>;
    getLookupGivingTypes(): Promise<NamedLookupRes[]>;
    protected processGetLookupGivingTypes(response: Response): Promise<NamedLookupRes[]>;
    getLookupSponsorshipDurations(): Promise<SponsorshipDurationRes[]>;
    protected processGetLookupSponsorshipDurations(response: Response): Promise<SponsorshipDurationRes[]>;
    getLookupSponsorshipSchemes(): Promise<SponsorshipSchemeRes[]>;
    protected processGetLookupSponsorshipSchemes(response: Response): Promise<SponsorshipSchemeRes[]>;
    getPrice(criteria: PriceCriteria): Promise<PriceRes>;
    protected processGetPrice(response: Response): Promise<PriceRes>;
    setCurrency(currencyCode: string): Promise<CurrencyRes>;
    protected processSetCurrency(response: Response): Promise<CurrencyRes>;
    getAllLookups(criteria: LookupsCriteria): Promise<GivingLookupsRes>;
    protected processGetAllLookups(response: Response): Promise<GivingLookupsRes>;
}
export interface DonationFormRes {
    title?: string | undefined;
    options?: DonationOptionRes[] | undefined;
}
export interface DonationOptionRes {
    id?: number;
    name?: string | undefined;
    campaignName?: string | undefined;
    type?: AllocationType | undefined;
    defaultGivingType?: GivingType | undefined;
    dimension1?: InitialFundDimensionValueRes | undefined;
    dimension2?: InitialFundDimensionValueRes | undefined;
    dimension3?: InitialFundDimensionValueRes | undefined;
    dimension4?: InitialFundDimensionValueRes | undefined;
    hideQuantity?: boolean;
    hideDonation?: boolean;
    hideRegularGiving?: boolean;
    fund?: FundDonationOptionRes | undefined;
    sponsorship?: SponsorshipDonationOptionRes | undefined;
    feedback?: FeedbackDonationOptionRes | undefined;
}
/** One of 'feedback', 'fund', 'sponsorship' */
export declare enum AllocationType {
    Feedback = "feedback",
    Fund = "fund",
    Sponsorship = "sponsorship"
}
/** One of 'donation', 'regularGiving' */
export declare enum GivingType {
    Donation = "donation",
    RegularGiving = "regularGiving"
}
export interface InitialFundDimensionValueRes {
    fixed?: FundDimensionValueRes | undefined;
    default?: FundDimensionValueRes | undefined;
    suggested?: FundDimensionValueRes | undefined;
}
export interface FundDimensionValueRes {
    name?: string | undefined;
    id?: string | undefined;
    isUnrestricted?: boolean;
}
export interface FundDonationOptionRes {
    donationItem?: string | undefined;
    donationPriceHandles?: PriceHandleRes[] | undefined;
    regularGivingPriceHandles?: PriceHandleRes[] | undefined;
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
export interface PriceHandleRes {
    amount?: number;
    currencyValues?: {
        [key: string]: MoneyRes;
    } | undefined;
    description?: string | undefined;
}
export interface MoneyRes {
    amount?: number;
    currency?: string | undefined;
    text?: string | undefined;
}
export interface SponsorshipDonationOptionRes {
    scheme?: string | undefined;
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
export interface FeedbackDonationOptionRes {
    scheme?: string | undefined;
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
export interface ProblemDetails {
    type?: string | undefined;
    title?: string | undefined;
    status?: number | undefined;
    detail?: string | undefined;
    instance?: string | undefined;
    [key: string]: any;
}
export interface FundStructureRes {
    dimension1?: FundDimensionRes | undefined;
    dimension2?: FundDimensionRes | undefined;
    dimension3?: FundDimensionRes | undefined;
    dimension4?: FundDimensionRes | undefined;
}
export interface FundDimensionRes {
    name?: string | undefined;
    id?: string | undefined;
    index?: number;
    isActive?: boolean;
    options?: FundDimensionValueRes[] | undefined;
}
export interface NamedLookupRes {
    id?: string | undefined;
    name?: string | undefined;
}
export interface CurrencyRes {
    name?: string | undefined;
    id?: string | undefined;
    code?: string | undefined;
    symbol?: string | undefined;
    icon?: string | undefined;
    decimalDigits?: number;
    isBaseCurrency?: boolean;
}
export interface DonationItemRes {
    name?: string | undefined;
    id?: string | undefined;
    allowedGivingTypes?: GivingType[] | undefined;
    fundDimensionOptions?: FundDimensionOptionsRes | undefined;
    pricing?: PricingRes | undefined;
}
export interface FundDimensionOptionsRes {
    dimension1?: string[] | undefined;
    dimension2?: string[] | undefined;
    dimension3?: string[] | undefined;
    dimension4?: string[] | undefined;
}
export interface PricingRes {
    price?: PriceRes | undefined;
    priceRules?: PricingRuleRes[] | undefined;
}
export interface PriceRes {
    amount?: number;
    currencyValues?: {
        [key: string]: MoneyRes;
    } | undefined;
    locked?: boolean;
}
export interface PricingRuleRes {
    price?: PriceRes | undefined;
    fundDimensions?: FundDimensionValuesRes | undefined;
}
export interface FundDimensionValuesRes {
    dimension1?: string | undefined;
    dimension2?: string | undefined;
    dimension3?: string | undefined;
    dimension4?: string | undefined;
}
export interface FeedbackSchemeRes {
    name?: string | undefined;
    id?: string | undefined;
    allowedGivingTypes?: GivingType[] | undefined;
    customFields?: FeedbackCustomFieldDefinitionRes[] | undefined;
    fundDimensionOptions?: FundDimensionOptionsRes | undefined;
    pricing?: PricingRes | undefined;
}
export interface FeedbackCustomFieldDefinitionRes {
    type?: FeedbackCustomFieldType | undefined;
    alias?: string | undefined;
    name?: string | undefined;
    required?: boolean;
    textMaxLength?: number | undefined;
}
export interface SponsorshipDurationRes {
    name?: string | undefined;
    id?: string | undefined;
    months?: number;
}
export interface SponsorshipSchemeRes {
    name?: string | undefined;
    id?: string | undefined;
    allowedGivingTypes?: GivingType[] | undefined;
    allowedDurations?: SponsorshipDuration[] | undefined;
    fundDimensionOptions?: FundDimensionOptionsRes | undefined;
    components?: SponsorshipComponentRes[] | undefined;
}
export interface SponsorshipComponentRes {
    name?: string | undefined;
    id?: string | undefined;
    pricing?: PricingRes | undefined;
    mandatory?: boolean;
}
export interface PriceCriteria {
    donationItem?: string | undefined;
    feedbackScheme?: string | undefined;
    sponsorshipComponent?: string | undefined;
    fundDimensions?: FundDimensionValuesReq | undefined;
}
export interface FundDimensionValuesReq {
    dimension1?: string | undefined;
    dimension2?: string | undefined;
    dimension3?: string | undefined;
    dimension4?: string | undefined;
}
export interface GivingLookupsRes {
    allocationTypes?: NamedLookupRes[] | undefined;
    currencies?: CurrencyRes[] | undefined;
    donationItems?: DonationItemRes[] | undefined;
    givingTypes?: NamedLookupRes[] | undefined;
    fundDimension1Values?: FundDimensionValueRes[] | undefined;
    fundDimension2Values?: FundDimensionValueRes[] | undefined;
    fundDimension3Values?: FundDimensionValueRes[] | undefined;
    fundDimension4Values?: FundDimensionValueRes[] | undefined;
    sponsorshipDurations?: SponsorshipDurationRes[] | undefined;
    sponsorshipSchemes?: SponsorshipSchemeRes[] | undefined;
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