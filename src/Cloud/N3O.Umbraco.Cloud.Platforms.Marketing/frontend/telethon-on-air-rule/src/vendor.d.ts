// Hand-written and never checked against Engage's class: installing @umbraco-engage/backoffice nests a
// second @umbraco-cms/backoffice (17.0.0) under this package.

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

    export abstract class UeSegmentRuleBaseElement extends UmbLitElement {
        readonly?: boolean;
        manifest?: UeSegmentRuleManifest;
        pending?: UeSegmentRuleValue;
        value?: UeSegmentRuleValue;

        abstract renderReadOnly(): unknown;
        abstract renderEditor(): unknown;
    }
}
