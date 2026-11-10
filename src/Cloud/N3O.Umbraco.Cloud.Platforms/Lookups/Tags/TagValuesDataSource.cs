using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.Contentment.DataEditors;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class TagValuesDataSource : IContentmentDataSource {
    public const string TagKeyField = "tagKey";

    private readonly ITagValuesAccessor _tagValuesAccessor;

    public TagValuesDataSource(ITagValuesAccessor tagValuesAccessor) {
        _tagValuesAccessor = tagValuesAccessor;
    }

    public string Name => "Tag Values";
    public string Description => "Data source for the published values of a tag";
    public string Icon => "icon-tags";
    public Dictionary<string, object> DefaultValues => null;
    public IEnumerable<ConfigurationField> Fields => [GetTagKeyField()];
    public string Group => "N3O";
    public OverlaySize OverlaySize => OverlaySize.Small;

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config) {
        var tagKey = config.GetOrDefault(TagKeyField)?.ToString();
        var publishedTagValues = _tagValuesAccessor.Get();

        var entry = publishedTagValues.OrEmpty(x => x.Entries).SingleOrDefault(x => x.Key.EqualsInvariant(tagKey));

        return entry.OrEmpty(x => x.Values).Select(ToDataListItem).ToList();
    }

    public Type GetValueType(Dictionary<string, object> config) {
        return typeof(string);
    }

    public object ConvertValue(Type type, string value) {
        return value;
    }

    private ConfigurationField GetTagKeyField() {
        var field = new ConfigurationField();
        field.Key = TagKeyField;
        field.Name = "Tag";
        field.Description = "The key of the tag whose published values are offered";
        field.View = "textstring";

        return field;
    }

    private DataListItem ToDataListItem(string value) {
        var dataListItem = new DataListItem();
        dataListItem.Name = value;
        dataListItem.Icon = Icon;
        dataListItem.Value = value;
        dataListItem.Group = Group;

        return dataListItem;
    }
}
