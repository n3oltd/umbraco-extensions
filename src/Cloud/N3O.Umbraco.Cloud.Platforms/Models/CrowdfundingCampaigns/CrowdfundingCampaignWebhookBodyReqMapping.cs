using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class CrowdfundingCampaignWebhookBodyReqMapping : IMapDefinition {
    public const string CampaignKeyContext = nameof(CampaignKeyContext);
    public const string CrowdfunderPageContentContext = nameof(CrowdfunderPageContentContext);
    public const string CrowdfundingCampaignPageContentContext = nameof(CrowdfundingCampaignPageContentContext);

    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<CrowdfundingCampaignContent, CrowdfundingCampaignWebhookBodyReq>((_, _) => new CrowdfundingCampaignWebhookBodyReq(), Map);
    }

    // Umbraco.Code.MapAll
    private void Map(CrowdfundingCampaignContent src, CrowdfundingCampaignWebhookBodyReq dest, MapperContext ctx) {
        dest.CampaignId = ((Guid) ctx.Items[CampaignKeyContext]).ToString();
        dest.Action = WebhookSyncAction.AddOrUpdate;

        dest.AddOrUpdate = GetCrowdfundingCampaignReq(src, ctx);
    }

    private CrowdfundingCampaignReq GetCrowdfundingCampaignReq(CrowdfundingCampaignContent src, MapperContext ctx) {
        var req = new CrowdfundingCampaignReq();
        req.Enable = true;

        var items = new List<StoredContentReq>();

        items.Add(GetStoredContent(src,
                                   ctx,
                                   CrowdfundingSystemSchema.Sys__crowdfunderPage,
                                   CrowdfunderPageContentContext));

        if (ctx.Items.ContainsKey(CrowdfundingCampaignPageContentContext)) {
            items.Add(GetStoredContent(src,
                                       ctx,
                                       CrowdfundingSystemSchema.Sys__crowdfundingCampaignPage,
                                       CrowdfundingCampaignPageContentContext));
        }

        req.StoredContents = new StoredContentsReq();
        req.StoredContents.Items = items;

        return req;
    }

    private StoredContentReq GetStoredContent(CrowdfundingCampaignContent src,
                                              MapperContext ctx,
                                              CrowdfundingSystemSchema schema,
                                              string contextKey) {
        var schemaAlias = schema.ToEnumString();

        var content = new ContentReq();
        content.SchemaAlias = schemaAlias;

        if (ctx.Items.TryGetValue(contextKey, out var value)) {
            var properties = ((IEnumerable<PropertyContentReq>) value).OrEmpty().Where(x => x.EditorHasValue());

            content.Properties = properties.ToList();
        }

        var req = new StoredContentReq();
        req.Id = GetStoredContentId(src.Key, schemaAlias);
        req.Content = content;

        return req;
    }

    private static string GetStoredContentId(Guid crowdfundingCampaignKey, string schemaAlias) {
        return $"StoredContent_{crowdfundingCampaignKey}_{schemaAlias}".GetDeterministicHashCode(true)
                                                                    .ToGuid()
                                                                    .ToString();
    }
}
