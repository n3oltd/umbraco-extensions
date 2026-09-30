export declare class CrowdfundingClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    getCampaignGoalOptions(campaignId: string, goalOptionId: string, crowdfunding_API_Key: string | undefined): Promise<GoalOptionRes>;
    protected processGetCampaignGoalOptions(response: Response): Promise<GoalOptionRes>;
    addToCart(crowdfunding_API_Key: string | undefined, crowdfundingReq: CrowdfundingCartReq): Promise<void>;
    protected processAddToCart(response: Response): Promise<void>;
    getContentPropertyValue(contentId: string, propertyAlias: string, crowdfunding_API_Key: string | undefined): Promise<ContentPropertyValueRes>;
    protected processGetContentPropertyValue(response: Response): Promise<ContentPropertyValueRes>;
    getNestedPropertySchema(contentId: string, propertyAlias: string, crowdfunding_API_Key: string | undefined): Promise<NestedSchemaRes>;
    protected processGetNestedPropertySchema(response: Response): Promise<NestedSchemaRes>;
    updateProperty(contentId: string, crowdfunding_API_Key: string | undefined, req: ContentPropertyReq): Promise<void>;
    protected processUpdateProperty(response: Response): Promise<void>;
    suggestSlug(name: string | null | undefined, crowdfunding_API_Key: string | undefined): Promise<string>;
    protected processSuggestSlug(response: Response): Promise<string>;
    activateFundraiser(fundraiserId: string, crowdfunding_API_Key: string | undefined): Promise<void>;
    protected processActivateFundraiser(response: Response): Promise<void>;
    createFundraiser(crowdfunding_API_Key: string | undefined, req: CreateFundraiserReq): Promise<string>;
    protected processCreateFundraiser(response: Response): Promise<string>;
    deactivateFundraiser(fundraiserId: string, crowdfunding_API_Key: string | undefined): Promise<void>;
    protected processDeactivateFundraiser(response: Response): Promise<void>;
    getFundraiserGoals(fundraiserId: string, crowdfunding_API_Key: string | undefined): Promise<FundraiserGoalsRes>;
    protected processGetFundraiserGoals(response: Response): Promise<FundraiserGoalsRes>;
    updateFundraiserGoals(fundraiserId: string, crowdfunding_API_Key: string | undefined, req: FundraiserGoalsReq): Promise<void>;
    protected processUpdateFundraiserGoals(response: Response): Promise<void>;
    getPropertyTypes(crowdfunding_API_Key: string | undefined): Promise<LookupRes[]>;
    protected processGetPropertyTypes(response: Response): Promise<LookupRes[]>;
}
export interface GoalOptionRes {
    id?: string | undefined;
    name?: string | undefined;
    type?: AllocationType | undefined;
    tags?: TagRes[] | undefined;
    dimension1?: GoalOptionFundDimensionRes | undefined;
    dimension2?: GoalOptionFundDimensionRes | undefined;
    dimension3?: GoalOptionFundDimensionRes | undefined;
    dimension4?: GoalOptionFundDimensionRes | undefined;
    fund?: DonationItemRes | undefined;
    feedback?: FeedbackSchemeRes | undefined;
}
/** One of 'feedback', 'fund', 'sponsorship' */
export declare enum AllocationType {
    Feedback = "feedback",
    Fund = "fund",
    Sponsorship = "sponsorship"
}
export interface TagRes {
    name?: string | undefined;
    iconUrl?: string | undefined;
}
export interface GoalOptionFundDimensionRes {
    default?: FundDimensionValueRes | undefined;
    allowedOptions?: FundDimensionValueRes[] | undefined;
}
export interface FundDimensionValueRes {
    name?: string | undefined;
    id?: string | undefined;
    isUnrestricted?: boolean;
}
export interface DonationItemRes {
    name?: string | undefined;
    id?: string | undefined;
    allowedGivingTypes?: GivingType[] | undefined;
    dimension1Options?: FundDimensionValueRes[] | undefined;
    dimension2Options?: FundDimensionValueRes[] | undefined;
    dimension3Options?: FundDimensionValueRes[] | undefined;
    dimension4Options?: FundDimensionValueRes[] | undefined;
    pricing?: PricingRes | undefined;
}
/** One of 'donation', 'regularGiving' */
export declare enum GivingType {
    Donation = "donation",
    RegularGiving = "regularGiving"
}
export interface PricingRes {
    amount?: number;
    currencyValues?: {
        [key: string]: MoneyRes;
    } | undefined;
    locked?: boolean;
    priceRules?: PricingRuleRes[] | undefined;
}
export interface MoneyRes {
    amount?: number;
    currency?: string | undefined;
    text?: string | undefined;
}
export interface PricingRuleRes {
    amount?: number;
    currencyValues?: {
        [key: string]: MoneyRes;
    } | undefined;
    locked?: boolean;
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
    dimension1Options?: FundDimensionValueRes[] | undefined;
    dimension2Options?: FundDimensionValueRes[] | undefined;
    dimension3Options?: FundDimensionValueRes[] | undefined;
    dimension4Options?: FundDimensionValueRes[] | undefined;
    pricing?: PricingRes | undefined;
}
export interface FeedbackCustomFieldDefinitionRes {
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
export interface CrowdfundingCartReq {
    items?: CrowdfundingCartItemReq[] | undefined;
    type?: CrowdfunderType | undefined;
    crowdfunding?: CrowdfunderDataReq | undefined;
}
export interface CrowdfundingCartItemReq {
    goalId?: string | undefined;
    value?: MoneyReq | undefined;
    feedback?: FeebackCrowdfundingCartItemReq | undefined;
}
export interface MoneyReq {
    amount?: number | undefined;
    currency?: string | undefined;
}
export interface FeebackCrowdfundingCartItemReq {
    customFields?: FeedbackNewCustomFieldsReq | undefined;
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
/** One of 'campaign', 'fundraiser' */
export declare enum CrowdfunderType {
    Campaign = "campaign",
    Fundraiser = "fundraiser"
}
export interface CrowdfunderDataReq {
    crowdfunderId?: string | undefined;
    comment?: string | undefined;
    anonymous?: boolean | undefined;
}
export interface ContentPropertyValueRes {
    alias?: string | undefined;
    type?: PropertyType | undefined;
    boolean?: BooleanValueRes | undefined;
    cropper?: CropperValueRes | undefined;
    dateTime?: DateTimeValueRes | undefined;
    nested?: NestedValueRes | undefined;
    numeric?: NumericValueRes | undefined;
    raw?: RawValueRes | undefined;
    textarea?: TextareaValueRes | undefined;
    textBox?: TextBoxValueRes | undefined;
}
/** One of 'boolean', 'cropper', 'dateTime', 'nested', 'numeric', 'raw', 'textarea', 'textBox' */
export declare enum PropertyType {
    Boolean = "boolean",
    Cropper = "cropper",
    DateTime = "dateTime",
    Nested = "nested",
    Numeric = "numeric",
    Raw = "raw",
    Textarea = "textarea",
    TextBox = "textBox"
}
export interface BooleanValueRes {
    value?: boolean | undefined;
    configuration?: BooleanConfigurationRes | undefined;
}
export interface BooleanConfigurationRes {
    description?: string | undefined;
}
export interface CropperValueRes {
    image?: CropperSource | undefined;
    storageToken?: string | undefined;
    configuration?: CropperConfigurationRes | undefined;
}
export interface CropperSource {
    src?: string | undefined;
    mediaId?: string | undefined;
    filename?: string | undefined;
    width?: number;
    height?: number;
    altText?: string | undefined;
    crops?: Crop[] | undefined;
}
export interface Crop {
    x?: number;
    y?: number;
    width?: number;
    height?: number;
}
export interface ByteSize {
    bits?: number;
    bytes?: number;
    kilobytes?: number;
    megabytes?: number;
    gigabytes?: number;
    terabytes?: number;
    largestWholeNumberSymbol?: string | undefined;
    largestWholeNumberFullWord?: string | undefined;
    largestWholeNumberValue?: number;
}
export interface CropperConfigurationRes {
    description?: string | undefined;
    rectangle?: RectangleCropConfigurationRes | undefined;
}
export interface RectangleCropConfigurationRes {
    height?: number;
    width?: number;
}
export interface DateTimeValueRes {
    value?: Date | undefined;
    configuration?: DateTimeConfigurationRes | undefined;
}
export interface DateTimeConfigurationRes {
    description?: string | undefined;
}
export interface NestedValueRes {
    items?: NestedItemRes[] | undefined;
    schema?: NestedSchemaRes | undefined;
    configuration?: NestedConfigurationRes | undefined;
}
export interface NestedItemRes {
    contentTypeAlias?: string | undefined;
    properties?: ContentPropertyValueRes[] | undefined;
}
export interface NestedSchemaRes {
    items?: NestedSchemaItemRes[] | undefined;
}
export interface NestedSchemaItemRes {
    contentTypeAlias?: string | undefined;
    properties?: NestedSchemaPropertyRes[] | undefined;
}
export interface NestedSchemaPropertyRes {
    alias?: string | undefined;
    type?: PropertyType | undefined;
    configuration?: ContentPropertyConfigurationRes | undefined;
}
export interface ContentPropertyConfigurationRes {
    description?: string | undefined;
}
export interface NestedConfigurationRes {
    description?: string | undefined;
    maximumItems?: number;
    minimumItems?: number;
}
export interface NumericValueRes {
    value?: number | undefined;
    configuration?: NumericConfigurationRes | undefined;
}
export interface NumericConfigurationRes {
    description?: string | undefined;
}
export interface RawValueRes {
    value?: HtmlEncodedString | undefined;
    configuration?: RawConfigurationRes | undefined;
}
export interface HtmlEncodedString {
}
export interface RawConfigurationRes {
    description?: string | undefined;
    maximumLength?: number;
}
export interface TextareaValueRes {
    value?: string | undefined;
    configuration?: TextareaConfigurationRes | undefined;
}
export interface TextareaConfigurationRes {
    description?: string | undefined;
    maximumLength?: number;
}
export interface TextBoxValueRes {
    value?: string | undefined;
    configuration?: TextBoxConfigurationRes | undefined;
}
export interface TextBoxConfigurationRes {
    description?: string | undefined;
    maximumLength?: number;
}
export interface ContentPropertyReq {
    alias?: string | undefined;
    type?: PropertyType | undefined;
    boolean?: BooleanValueReq | undefined;
    cropper?: CropperValueReq | undefined;
    dateTime?: DateTimeValueReq | undefined;
    nested?: NestedValueReq | undefined;
    numeric?: NumericValueReq | undefined;
    raw?: RawValueReq | undefined;
    textarea?: TextareaValueReq | undefined;
    textBox?: TextBoxValueReq | undefined;
}
export interface BooleanValueReq {
    value?: boolean | undefined;
}
export interface CropperValueReq {
    type?: PropertyType | undefined;
    storageToken?: string | undefined;
    shape?: CropShape | undefined;
    circle?: CircleCropReq | undefined;
    rectangle?: RectangleCropReq | undefined;
}
/** One of 'circle', 'rectangle' */
export declare enum CropShape {
    Circle = "circle",
    Rectangle = "rectangle"
}
export interface CircleCropReq {
    center?: PointReq | undefined;
    radius?: number | undefined;
}
export interface PointReq {
    x?: number | undefined;
    y?: number | undefined;
}
export interface RectangleCropReq {
    bottomLeft?: PointReq | undefined;
    topRight?: PointReq | undefined;
}
export interface DateTimeValueReq {
    value?: Date | undefined;
}
export interface NestedValueReq {
    items?: NestedItemReq[] | undefined;
}
export interface NestedItemReq {
    contentTypeAlias?: string | undefined;
    properties?: ContentPropertyReq[] | undefined;
}
export interface NumericValueReq {
    value?: number | undefined;
}
export interface RawValueReq {
    value?: string | undefined;
}
export interface TextareaValueReq {
    value?: string | undefined;
}
export interface TextBoxValueReq {
    value?: string | undefined;
}
export interface AutoPropertyOfValueReq {
    value?: ValueReq | undefined;
}
export interface ValueReq {
    type?: PropertyType | undefined;
}
export interface CreateFundraiserReq {
    name?: string | undefined;
    slug?: string | undefined;
    campaignId?: string | undefined;
    currency?: string | undefined;
    goals?: FundraiserGoalsReq | undefined;
}
export interface FundraiserGoalsReq {
    items?: FundraiserGoalReq[] | undefined;
}
export interface FundraiserGoalReq {
    amount?: number | undefined;
    goalOptionId?: string | undefined;
    fundDimensions?: FundDimensionValuesReq | undefined;
    feedback?: FeedbackGoalReq | undefined;
}
export interface FundDimensionValuesReq {
    dimension1?: string | undefined;
    dimension2?: string | undefined;
    dimension3?: string | undefined;
    dimension4?: string | undefined;
}
export interface FeedbackGoalReq {
    customFields?: FeedbackNewCustomFieldsReq | undefined;
}
export interface FundraiserGoalsRes {
    currency?: CurrencyRes | undefined;
    minimumValues?: {
        [key: string]: MoneyRes;
    } | undefined;
    goalOptions?: GoalOptionRes[] | undefined;
    selectedGoals?: GoalRes[] | undefined;
}
export interface CurrencyRes {
    name?: string | undefined;
    id?: string | undefined;
    code?: string | undefined;
    isBaseCurrency?: boolean;
    symbol?: string | undefined;
}
export interface GoalRes {
    optionId?: string | undefined;
    value?: number;
    fundDimensions?: FundDimensionValuesRes | undefined;
    feedback?: FeedbackGoalRes | undefined;
    tags?: TagRes[] | undefined;
}
export interface FeedbackGoalRes {
    feedback?: FeedbackCustomFieldRes[] | undefined;
}
export interface FeedbackCustomFieldRes {
    type?: FeedbackCustomFieldType | undefined;
    alias?: string | undefined;
    name?: string | undefined;
    bool?: boolean | undefined;
    date?: string | undefined;
    text?: string | undefined;
}
export interface LookupRes {
    id?: string | undefined;
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