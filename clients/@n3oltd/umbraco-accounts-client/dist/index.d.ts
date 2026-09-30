export declare class AccountsClient {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    getDataEntrySettings(): Promise<DataEntrySettings>;
    protected processGetDataEntrySettings(response: Response): Promise<DataEntrySettings>;
    getLookupConsentCategories(): Promise<NamedLookupRes[]>;
    protected processGetLookupConsentCategories(response: Response): Promise<NamedLookupRes[]>;
    getLookupConsentChannels(): Promise<NamedLookupRes[]>;
    protected processGetLookupConsentChannels(response: Response): Promise<NamedLookupRes[]>;
    getLookupConsentResponses(): Promise<NamedLookupRes[]>;
    protected processGetLookupConsentResponses(response: Response): Promise<NamedLookupRes[]>;
    getLookupCountries(): Promise<CountryRes[]>;
    protected processGetLookupCountries(response: Response): Promise<CountryRes[]>;
    getLookupTaxStatuses(): Promise<NamedLookupRes[]>;
    protected processGetLookupTaxStatuses(response: Response): Promise<NamedLookupRes[]>;
    getAllLookups(criteria: LookupsCriteria): Promise<AccountsLookupsRes>;
    protected processGetAllLookups(response: Response): Promise<AccountsLookupsRes>;
}
export interface DataEntrySettings {
    name?: NameDataEntrySettings | undefined;
    address?: AddressDataEntrySettings | undefined;
    email?: EmailDataEntrySettings | undefined;
    phone?: PhoneDataEntrySettings | undefined;
    consent?: ConsentDataEntrySettings | undefined;
}
export interface NameDataEntrySettings {
    title?: SelectFieldSettings | undefined;
    firstName?: TextFieldSettings | undefined;
    lastName?: TextFieldSettings | undefined;
}
export interface SelectFieldSettings {
    required?: boolean;
    label?: string | undefined;
    helpText?: string | undefined;
    order?: number;
    path?: string | undefined;
    visible?: boolean;
    validate?: boolean;
    type?: string | undefined;
    options?: SelectOption[] | undefined;
    defaultOption?: SelectOption | undefined;
}
export interface SelectOption {
    value?: string | undefined;
    text?: string | undefined;
}
export interface TextFieldSettings {
    required?: boolean;
    label?: string | undefined;
    helpText?: string | undefined;
    order?: number;
    path?: string | undefined;
    visible?: boolean;
    validate?: boolean;
    type?: string | undefined;
    capitalisation?: Capitalisation | undefined;
}
/** One of 'lower', 'title', 'upper' */
export declare enum Capitalisation {
    Lower = "lower",
    Title = "title",
    Upper = "upper"
}
export interface AddressDataEntrySettings {
    country?: SelectFieldSettings | undefined;
    line1?: TextFieldSettings | undefined;
    line2?: TextFieldSettings | undefined;
    line3?: TextFieldSettings | undefined;
    locality?: TextFieldSettings | undefined;
    administrativeArea?: TextFieldSettings | undefined;
    postalCode?: TextFieldSettings | undefined;
    defaultCountry?: string | undefined;
}
export interface EmailDataEntrySettings {
    capitalisation?: Capitalisation | undefined;
    type?: string | undefined;
    required?: boolean;
    label?: string | undefined;
    helpText?: string | undefined;
    order?: number;
    path?: string | undefined;
    visible?: boolean;
    validate?: boolean;
}
export interface PhoneDataEntrySettings {
    country?: SelectFieldSettings | undefined;
    number?: PhoneFieldSettings | undefined;
}
export interface PhoneFieldSettings {
    required?: boolean;
    label?: string | undefined;
    helpText?: string | undefined;
    order?: number;
    path?: string | undefined;
    visible?: boolean;
    validate?: boolean;
    type?: string | undefined;
}
export interface ConsentDataEntrySettings {
    consentOptions?: ConsentOption[] | undefined;
}
export interface ConsentOption {
    channel?: ConsentChannel | undefined;
    categories?: string[] | undefined;
    statement?: string | undefined;
}
/** One of 'email', 'sms', 'post', 'telephone' */
export declare enum ConsentChannel {
    Email = "email",
    Sms = "sms",
    Post = "post",
    Telephone = "telephone"
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
export interface CountryRes {
    name?: string | undefined;
    id?: string | undefined;
    iso2Code?: string | undefined;
    iso3Code?: string | undefined;
    diallingCode?: string | undefined;
    localityOptional?: boolean;
    postalCodeOptional?: boolean;
}
export interface AccountsLookupsRes {
    consentCategories?: NamedLookupRes[] | undefined;
    consentChannels?: NamedLookupRes[] | undefined;
    consentResponses?: NamedLookupRes[] | undefined;
    countries?: CountryRes[] | undefined;
    taxStatuses?: NamedLookupRes[] | undefined;
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