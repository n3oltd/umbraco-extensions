// Type stub for the Engage backoffice module that Engage serves through its import map. The
// @umbraco-engage/backoffice npm package pins its own @umbraco-cms/backoffice version, so it is not installed.

declare module '@umbraco-engage/backoffice/personalization' {
    import type { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

    export interface UeSegmentRuleValue {
        config: Record<string, unknown>;
        isNegation: boolean;
    }

    export interface UeSegmentRuleManifest {
        elementName: string;
        meta: {
            name: string;
            type: string;
            icon: string;
            config: Record<string, unknown>;
        };
    }

    export class UeSegmentRuleBaseElement extends UmbLitElement {
        readonly: boolean;
        manifest?: UeSegmentRuleManifest;
        pending?: UeSegmentRuleValue;
        value?: UeSegmentRuleValue;
    }
}
