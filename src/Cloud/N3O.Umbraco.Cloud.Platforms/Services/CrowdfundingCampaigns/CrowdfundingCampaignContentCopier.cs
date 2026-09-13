using N3O.Umbraco.Blocks;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;
using CampaignProperties = N3O.Umbraco.Cloud.Platforms.PlatformsConstants.Campaigns.Properties;
using CrowdfundingCampaignProperties = N3O.Umbraco.Cloud.Platforms.PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Properties;

namespace N3O.Umbraco.Cloud.Platforms;

public class CrowdfundingCampaignContentCopier : ICrowdfundingCampaignContentCopier {
    private readonly IEnumerable<IBlocksCloner> _cloners;

    public CrowdfundingCampaignContentCopier(IEnumerable<IBlocksCloner> cloners) {
        _cloners = cloners;
    }

    public void CopyFromCampaign(IContent crowdfundingCampaign, IContent campaign) {
        var fingerprints = new Dictionary<string, string>();

        foreach (var mapping in GetMappings()) {
            CopyProperty(crowdfundingCampaign, campaign, mapping.Source, mapping.Destinations, fingerprints);
        }

        SetStamp(crowdfundingCampaign, fingerprints);
    }

    public bool UpdateFromCampaign(IContent crowdfundingCampaign, IContent campaign) {
        var fingerprints = new Dictionary<string, string>(GetStamp(crowdfundingCampaign));

        if (fingerprints.None()) {
            return false;
        }

        var updated = false;

        foreach (var mapping in GetMappings()) {
            if (UpdateProperty(crowdfundingCampaign, campaign, mapping.Source, mapping.Destinations, fingerprints)) {
                updated = true;
            }
        }

        if (updated) {
            SetStamp(crowdfundingCampaign, fingerprints);
        }

        return updated;
    }

    private bool CanCopy(IContent content, string alias, out IProperty property) {
        property = content.HasProperty(alias) ? content.Properties[alias] : null;

        // SetValue throws without a culture on a property that varies by one
        return property != null && !property.PropertyType.VariesByCulture();
    }

    private string Canonicalise(string value) {
        if (!value.HasValue()) {
            return string.Empty;
        }

        if (!value.DetectIsJson()) {
            return value;
        }

        try {
            return SortProperties(JToken.Parse(value)).ToString(Formatting.None);
        } catch (JsonException) {
            return value;
        }
    }

    private string Clone(string editorAlias, string value) {
        var cloner = GetCloner(editorAlias);

        return cloner == null ? value : cloner.Clone(value);
    }

    private void CopyProperty(IContent crowdfundingCampaign,
                              IContent campaign,
                              string sourceAlias,
                              IEnumerable<string> destinationAliases,
                              IDictionary<string, string> fingerprints) {
        if (!CanCopy(campaign, sourceAlias, out var source)) {
            return;
        }

        var value = campaign.GetValue<string>(sourceAlias, published: true);

        foreach (var destinationAlias in destinationAliases) {
            var editorAlias = GetEditorAlias(crowdfundingCampaign, destinationAlias, source);

            if (!editorAlias.HasValue() || crowdfundingCampaign.GetValue<string>(destinationAlias).HasValue()) {
                continue;
            }

            var written = IsEmpty(editorAlias, value) ? null : Clone(editorAlias, value);

            if (written.HasValue()) {
                crowdfundingCampaign.SetValue(destinationAlias, written);
            }

            fingerprints[destinationAlias] = Fingerprint(editorAlias, written);
        }
    }

    private string Fingerprint(string editorAlias, string value) {
        var canonical = IsEmpty(editorAlias, value) ? string.Empty : Canonicalise(value);
        var cloner = GetCloner(editorAlias);

        return (cloner == null ? canonical : cloner.StripIdentifiers(canonical)).Sha256();
    }

    private IBlocksCloner GetCloner(string editorAlias) {
        return _cloners.FirstOrDefault(x => x.CanClone(editorAlias));
    }

    private string GetEditorAlias(IContent crowdfundingCampaign, string destinationAlias, IProperty source) {
        if (!CanCopy(crowdfundingCampaign, destinationAlias, out var destination)) {
            return null;
        }

        var editorAlias = destination.PropertyType.PropertyEditorAlias;

        return source.PropertyType.PropertyEditorAlias.EqualsInvariant(editorAlias) ? editorAlias : null;
    }

    private IReadOnlyList<(string Source, string[] Destinations)> GetMappings() {
        return [
            (CampaignProperties.HeroImage,
             [CrowdfundingCampaignProperties.PageHeroImage,
              CrowdfundingCampaignProperties.PageTemplateHeroImage]),
            (CampaignProperties.PageContent,
             [CrowdfundingCampaignProperties.PageContent,
              CrowdfundingCampaignProperties.PageTemplateContent]),
            (CampaignProperties.PageContentAdditional,
             [CrowdfundingCampaignProperties.PageContentAdditional,
              CrowdfundingCampaignProperties.PageTemplateContentAdditional])
        ];
    }

    private IReadOnlyDictionary<string, string> GetStamp(IContent crowdfundingCampaign) {
        var empty = new Dictionary<string, string>();
        var value = crowdfundingCampaign.GetContentSyncStamp();

        if (!value.HasValue()) {
            return empty;
        }

        try {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(value) ?? empty;
        } catch (JsonException) {
            return empty;
        }
    }

    private bool IsEmpty(string editorAlias, string value) {
        var cloner = GetCloner(editorAlias);

        if (!value.HasValue()) {
            return true;
        } else if (cloner != null) {
            return cloner.IsEmpty(value);
        } else {
            return Canonicalise(value).IsAnyOf("[]", "{}");
        }
    }

    private void SetStamp(IContent crowdfundingCampaign, IReadOnlyDictionary<string, string> fingerprints) {
        if (fingerprints.HasAny()) {
            crowdfundingCampaign.SetContentSyncStamp(JsonConvert.SerializeObject(fingerprints));
        } else {
            crowdfundingCampaign.SetContentSyncStamp(null);
        }
    }

    private JToken SortProperties(JToken token) {
        if (token is JObject json) {
            var sorted = new JObject();

            foreach (var property in json.Properties().OrderBy(x => x.Name, StringComparer.Ordinal)) {
                sorted[property.Name] = SortProperties(property.Value);
            }

            return sorted;
        }

        if (token is JArray array) {
            return new JArray(array.Select(x => SortProperties(x)));
        }

        return token;
    }

    private bool UpdateProperty(IContent crowdfundingCampaign,
                                IContent campaign,
                                string sourceAlias,
                                IEnumerable<string> destinationAliases,
                                IDictionary<string, string> fingerprints) {
        if (!CanCopy(campaign, sourceAlias, out var source)) {
            return false;
        }

        var value = campaign.GetValue<string>(sourceAlias, published: true);

        if (IsEmpty(source.PropertyType.PropertyEditorAlias, value)) {
            return false;
        }

        var updated = false;

        foreach (var destinationAlias in destinationAliases) {
            var editorAlias = GetEditorAlias(crowdfundingCampaign, destinationAlias, source);

            if (!editorAlias.HasValue() || !fingerprints.TryGetValue(destinationAlias, out var stamped)) {
                continue;
            }

            var current = crowdfundingCampaign.GetValue<string>(destinationAlias);

            if (!Fingerprint(editorAlias, current).EqualsInvariant(stamped)) {
                continue;
            }

            var written = Clone(editorAlias, value);
            var fingerprint = Fingerprint(editorAlias, written);

            if (fingerprint.EqualsInvariant(stamped)) {
                continue;
            }

            crowdfundingCampaign.SetValue(destinationAlias, written);
            fingerprints[destinationAlias] = fingerprint;
            updated = true;
        }

        return updated;
    }
}
