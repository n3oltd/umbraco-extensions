import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import type { UmbDocumentWorkspaceContext } from '@umbraco-cms/backoffice/document';
import { UmbVariantId } from '@umbraco-cms/backoffice/variant';

const campaignAlias = 'campaign';

const crowdfundingCampaignAlias = 'platformsCrowdfundingCampaign';

const newContentName = 'New Crowdfunding Campaign';

export class N3oCrowdfundingVisibilityContext extends UmbControllerBase {
    #workspaceContext?: UmbDocumentWorkspaceContext;
    #contentTypeAlias?: string;
    #isNew = false;
    #properties: Array<{ unique: string; alias: string }> = [];
    #ruleUniques: string[] = [];
    #values: Array<{ alias: string; value?: unknown }> = [];

    constructor(host: UmbControllerHost) {
        super(host);

        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
            this.#workspaceContext = context ?? undefined;

            if (!context) {
                return;
            }

            this.observe(context.isNew, (isNew) => {
                this.#isNew = isNew === true;
                this.#apply();
            }, '_n3oCrowdfundingIsNew');

            this.observe(context.structure.ownerContentTypeAlias, (alias) => {
                this.#contentTypeAlias = alias;
                this.#apply();
            }, '_n3oCrowdfundingContentType');

            this.observe(context.structure.contentTypeProperties, (properties) => {
                this.#properties = properties ?? [];
                this.#apply();
            }, '_n3oCrowdfundingProperties');

            this.observe(context.values, (values) => {
                this.#values = values ?? [];
                this.#apply();
            }, '_n3oCrowdfundingValues');
        });
    }

    #apply(): void {
        const context = this.#workspaceContext;

        if (!context) {
            return;
        }

        if (this.#ruleUniques.length) {
            context.propertyViewGuard.removeRules(this.#ruleUniques);
            this.#ruleUniques = [];
        }

        if (!this.#isNew || this.#contentTypeAlias !== crowdfundingCampaignAlias) {
            return;
        }

        const campaign = this.#properties.find((x) => x.alias === campaignAlias);

        if (!campaign || this.#values.some((x) => x.alias !== campaignAlias && this.#hasValue(x.value))) {
            return;
        }

        this.#setNewContentName(context);

        this.#properties
            .filter((property) => property !== campaign)
            .forEach((property) => {
                const ruleUnique = `n3o-crowdfunding-${property.unique}`;

                context.propertyViewGuard.addRule({
                    unique: ruleUnique,
                    permitted: false,
                    propertyType: { unique: property.unique },
                });

                this.#ruleUniques.push(ruleUnique);
            });
    }

    #hasValue(value: unknown): boolean {
        return value !== null && value !== undefined && (typeof value !== 'string' || value.trim().length > 0);
    }

    #setNewContentName(context: UmbDocumentWorkspaceContext): void {
        (context.getData()?.variants ?? [])
            .filter((variant) => !variant.name)
            .forEach((variant) => context.setName(newContentName, UmbVariantId.Create(variant)));
    }
}

export { N3oCrowdfundingVisibilityContext as api };
export default N3oCrowdfundingVisibilityContext;
