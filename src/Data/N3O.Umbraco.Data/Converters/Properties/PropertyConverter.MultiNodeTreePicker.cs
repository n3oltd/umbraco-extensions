using N3O.Umbraco.Content;
using N3O.Umbraco.Data.Builders;
using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Data.Parsing;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.DynamicRoot;
using Umbraco.Cms.Core.DynamicRoot.QuerySteps;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Extensions;
using OurDataTypes = N3O.Umbraco.Data.Lookups.DataTypes;
using UmbracoConstants = Umbraco.Cms.Core.Constants;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.Data.Converters;

public class MultiNodeTreePickerPropertyConverter : PropertyConverter<IPublishedContent> {
    // A converter is not given the document being imported, so origins relative to it cannot be resolved.
    private static readonly string[] ContextFreeOriginAliases = ["ByKey", "ContentRoot"];
    private static readonly string EditorAlias = UmbracoPropertyEditors.Aliases.MultiNodeTreePicker; 
    // Matches no published content, so a name or path cell fails rather than searching the whole tree.
    private static readonly Guid NoStartNodeId = Guid.Empty;
    private readonly IContentHelper _contentHelper;
    private readonly IDynamicRootService _dynamicRootService;
    private readonly IPublishedContentCache _publishedContentCache;

    public MultiNodeTreePickerPropertyConverter(IColumnRangeBuilder columnRangeBuilder,
                                                IContentHelper contentHelper,
                                                IDynamicRootService dynamicRootService,
                                                IPublishedContentCache publishedContentCache)
        : base(columnRangeBuilder) {
        _contentHelper = contentHelper;
        _dynamicRootService = dynamicRootService;
        _publishedContentCache = publishedContentCache;
    }
    
    public override bool IsConverter(UmbracoPropertyInfo propertyInfo) {
        if (!propertyInfo.Type.PropertyEditorAlias.EqualsInvariant(EditorAlias)) {
            return false;
        }
        
        var configuration = propertyInfo.DataType.ConfigurationAs<MultiNodePickerConfiguration>();
        var parentId = configuration.TreeSource?.StartNodeId;

        if (parentId == null && configuration.TreeSource.HasValue(x => x.StartNodeQuery)) {
            // As IPublishedQuery.ContentAtXPath() does not work without variables such as $root, $site etc.
            return false;
        }

        return true;
    }

    protected override IEnumerable<Cell<IPublishedContent>> GetCells(IContentProperty contentProperty,
                                                                     UmbracoPropertyInfo propertyInfo) {
        IEnumerable<IPublishedContent> values;

        if (GetMaxValues(propertyInfo) == 1) {
            values = _contentHelper.GetMultiNodeTreePickerValue<IPublishedContent>(contentProperty).Yield();
        } else {
            values = _contentHelper.GetMultiNodeTreePickerValues<IPublishedContent>(contentProperty);
        }

        return values.ExceptNull().Select(x => OurDataTypes.PublishedContent.Cell(x, x.GetType()));
    }

    public override void Import(IContentBuilder contentBuilder,
                                IEnumerable<IPropertyConverter> converters,
                                IParser parser,
                                ErrorLog errorLog,
                                string columnTitlePrefix,
                                UmbracoPropertyInfo propertyInfo,
                                IEnumerable<ImportField> fields) {
        var parentId = GetParentId(propertyInfo);

        ImportAll(errorLog,
                  propertyInfo,
                  fields,
                  s => parser.PublishedContent.Parse(s, OurDataTypes.PublishedContent.GetClrType(), parentId),
                  (alias, values) => contentBuilder.ContentPicker(alias).SetContent(values));
    }

    protected override int GetMaxValues(UmbracoPropertyInfo propertyInfo) {
        var configuration = propertyInfo.DataType.ConfigurationAs<MultiNodePickerConfiguration>();

        if (configuration.MaxNumber == 0) {
            return DataConstants.Limits.Columns.MaxValues;
        } else {
            return configuration.MaxNumber;
        }
    }
    
    private Guid? GetDynamicRootId(DynamicRoot dynamicRoot) {
        var context = new DynamicRootContext { CurrentKey = null, ParentKey = Guid.Empty };
        var query = new DynamicRootNodeQuery { OriginAlias = dynamicRoot.OriginAlias, Context = context };
        query.OriginKey = dynamicRoot.OriginKey;
        query.QuerySteps = dynamicRoot.QuerySteps.Select(GetQueryStep).ToList();

        var rootIds = _dynamicRootService.GetDynamicRootsAsync(query).GetAwaiter().GetResult();

        // The query also returns trashed and unpublished nodes.
        return rootIds.Where(IsSearchable).Select(x => (Guid?) x).FirstOrDefault();
    }

    private Guid? GetParentId(UmbracoPropertyInfo propertyInfo) {
        var configuration = propertyInfo.DataType.ConfigurationAs<MultiNodePickerConfiguration>();
        var treeSource = configuration.TreeSource;
        var dynamicRoot = treeSource?.DynamicRoot;

        if (treeSource?.StartNodeId != null || !IsContextFree(dynamicRoot)) {
            return treeSource?.StartNodeId;
        }

        var rootId = GetDynamicRootId(dynamicRoot);

        // The system root is not published content, so it cannot be passed as a start node.
        if (rootId == UmbracoConstants.System.RootSystemKey) {
            return null;
        } else {
            return rootId ?? NoStartNodeId;
        }
    }

    private DynamicRootQueryStep GetQueryStep(QueryStep queryStep) {
        var dynamicRootQueryStep = new DynamicRootQueryStep();
        dynamicRootQueryStep.Alias = queryStep.Alias;
        dynamicRootQueryStep.AnyOfDocTypeKeys = queryStep.AnyOfDocTypeKeys;

        return dynamicRootQueryStep;
    }

    private bool IsContextFree(DynamicRoot dynamicRoot) {
        return dynamicRoot.HasValue() && ContextFreeOriginAliases.Any(x => x.EqualsInvariant(dynamicRoot.OriginAlias));
    }

    private bool IsSearchable(Guid rootId) {
        return rootId == UmbracoConstants.System.RootSystemKey || _publishedContentCache.GetById(rootId).HasValue();
    }
}
