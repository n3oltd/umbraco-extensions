using Humanizer;
using N3O.Umbraco.Extensions;
using System.Linq;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Cloud.Platforms.Content;

public static class CampaignTagAlias {
    public static string For(string tagKey) {
        var words = Regex.Split(tagKey, "[^A-Za-z0-9]+").Where(x => x.HasValue()).Select(x => x.Pascalize());

        return $"tag{string.Concat(words)}";
    }
}
