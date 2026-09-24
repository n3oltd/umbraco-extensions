using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Net;
using System.Net.Sockets;

namespace N3O.Umbraco.Hosting;

public class StagingSettingsContentValidator : ContentValidator {
    private static readonly int MinIPv4PrefixLength = 16;
    private static readonly int MinIPv6PrefixLength = 48;
    private static readonly string RuleIpAddressAlias =
        AliasHelper<FirewallRuleElement>.PropertyAlias(x => x.RuleIpAddress);
    private static readonly string RulesAlias = AliasHelper<StagingSettingsContent>.PropertyAlias(x => x.Rules);
    private static readonly string StagingSettingsAlias = AliasHelper<StagingSettingsContent>.ContentTypeAlias();

    public StagingSettingsContentValidator(IContentHelper contentHelper) : base(contentHelper) { }

    public override bool IsValidator(ContentProperties content) {
        return content.ContentTypeAlias.EqualsInvariant(StagingSettingsAlias);
    }

    public override void Validate(ContentProperties content) {
        var rulesProperty = content.GetElementsPropertyByAlias(RulesAlias);

        foreach (var rule in rulesProperty.OrEmpty(x => x.Value)) {
            var ipAddressProperty = rule.GetPropertyByAlias(RuleIpAddressAlias);
            var ipAddress = ((string) ipAddressProperty.Value)?.Trim();

            if (!IPAddress.TryParse(ipAddress, out _)) {
                ValidateNetwork(ipAddressProperty, ipAddress);
            }
        }
    }

    private int GetMinPrefixLength(IPNetwork network) {
        if (network.BaseAddress.AddressFamily == AddressFamily.InterNetwork) {
            return MinIPv4PrefixLength;
        } else {
            return MinIPv6PrefixLength;
        }
    }

    private void ValidateNetwork(IContentProperty ipAddressProperty, string ipAddress) {
        if (!IPNetwork.TryParse(ipAddress, out var network)) {
            ErrorResult(ipAddressProperty,
                        $"contains '{ipAddress}', which is not an IP address or a CIDR range starting at its " +
                        "network address");
        } else if (!network.BaseAddress.Equals(IPAddress.Parse(ipAddress.Split('/')[0]))) {
            ErrorResult(ipAddressProperty, $"contains '{ipAddress}', which should be written as '{network}'");
        } else if (network.PrefixLength < GetMinPrefixLength(network)) {
            ErrorResult(ipAddressProperty,
                        $"contains '{ipAddress}', which is wider than /{GetMinPrefixLength(network)}");
        }
    }
}
