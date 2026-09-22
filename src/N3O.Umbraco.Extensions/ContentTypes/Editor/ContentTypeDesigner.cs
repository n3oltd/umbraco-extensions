using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace N3O.Umbraco.ContentTypes;

public abstract class ContentTypeDesigner : IContentTypeDesigner {
    private readonly IContentTypeService _contentTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly bool _isElement;
    private readonly List<string> _compositions = [];
    private readonly List<PropertyContainerBuilder> _containers = [];

    private string _description;
    private bool _deterministic;
    private string[] _folderPath = [];
    private string _icon;
    private Guid? _id;
    private string _name;
    private bool _overwriteDescription;
    private bool _overwriteIcon;
    private bool _overwriteName;
    private bool _varyByCulture;
    private bool _varyBySegment;

    protected ContentTypeDesigner(IServiceProvider serviceProvider,
                                  IContentTypeService contentTypeService,
                                  IShortStringHelper shortStringHelper,
                                  string name,
                                  string alias,
                                  bool isElement) {
        ServiceProvider = serviceProvider;
        _contentTypeService = contentTypeService;
        _shortStringHelper = shortStringHelper;
        _name = name;
        Alias = alias;
        _isElement = isElement;
    }

    public void AddComposition(string contentTypeAlias) {
        _compositions.Add(contentTypeAlias);
    }

    public void AddComposition<T>() {
        AddComposition(AliasHelper.ContentTypeAlias(typeof(T)));
    }

    public IPropertyContainerBuilder Group(string name) {
        var container = new PropertyContainerBuilder(ServiceProvider, _shortStringHelper, name, false, null);

        RegisterContainer(container);

        return container;
    }

    public void InFolder(params string[] path) {
        _folderPath = path;
    }

    public IContentType Save() {
        var existing = FindExisting();
        var contentType = existing ?? Create();

        if (existing == null) {
            contentType.IsElement = _isElement;
        } else if (existing.IsElement != _isElement) {
            throw new ContentTypeKindMismatchException(Alias, existing.IsElement);
        }

        if (existing == null || _overwriteName) {
            contentType.Name = _name;
        }

        if (_icon.HasValue() && (existing == null || _overwriteIcon)) {
            contentType.Icon = _icon;
        }

        if (_description.HasValue() && (existing == null || _overwriteDescription)) {
            contentType.Description = _description;
        }

        if (_varyByCulture) {
            contentType.Variations |= ContentVariation.Culture;
        }

        if (_varyBySegment) {
            contentType.Variations |= ContentVariation.Segment;
        }

        ApplyCompositions(contentType);
        ApplyContainers(contentType);
        ApplyKind(contentType);

        _contentTypeService.Save(contentType);

        return contentType;
    }

    public void SetDescription(string description, bool overwriteExisting = false) {
        _description = description;
        _overwriteDescription = overwriteExisting;
    }

    public void SetIcon(string icon, bool overwriteExisting = false) {
        _icon = icon;
        _overwriteIcon = overwriteExisting;
    }

    public void SetName(string name, bool overwriteExisting = false) {
        _name = name;
        _overwriteName = overwriteExisting;
    }

    public IPropertyContainerBuilder Tab(string name) {
        var container = new PropertyContainerBuilder(ServiceProvider, _shortStringHelper, name, true, null);

        RegisterContainer(container);

        return container;
    }

    public void VaryByCulture() {
        _varyByCulture = true;
    }

    public void VaryBySegment() {
        _varyBySegment = true;
    }

    public void WithDeterministicId() {
        _deterministic = true;
        _id = UmbracoId.Deterministic(IdScope.ContentType, Alias);
    }

    public void WithId(Guid id) {
        _deterministic = true;
        _id = id;
    }

    public string Alias { get; }

    protected virtual void ApplyKind(IContentType contentType) { }

    protected void RegisterContainer(PropertyContainerBuilder container) {
        _containers.Add(container);
    }

    protected IContentType ResolveContentType(string alias) {
        var contentType = _contentTypeService.Get(alias);

        if (contentType == null) {
            throw new ContentTypeNotFoundException(alias);
        }

        return contentType;
    }

    protected IServiceProvider ServiceProvider { get; }
    protected IShortStringHelper ShortStringHelper => _shortStringHelper;

    private void ApplyCompositions(IContentType contentType) {
        foreach (var alias in _compositions) {
            if (!contentType.ContentTypeCompositionExists(alias)) {
                contentType.AddContentType(ResolveContentType(alias));
            }
        }
    }

    private void ApplyContainer(IContentType contentType, PropertyContainerBuilder container, int sortOrder) {
        var containerType = container.IsTab ? PropertyGroupType.Tab : PropertyGroupType.Group;

        var group = contentType.PropertyGroups
                               .FirstOrDefault(x => x.Alias.EqualsInvariant(container.Alias) &&
                                                    x.Type == containerType);

        if (group == null) {
            var nested = contentType.PropertyGroups
                                    .Where(x => x.Type == containerType &&
                                                x.Alias.EndsWith($"/{container.Alias}",
                                                                 StringComparison.InvariantCultureIgnoreCase))
                                    .ToList();

            if (nested.Count == 1) {
                group = nested.Single();
            }
        }

        var isNeeded = container.Properties.Any(x => !contentType.PropertyTypeExists(x.Alias));

        if (group == null && isNeeded) {
            var parentTab = container.IsTab
                                ? null
                                : contentType.PropertyGroups
                                             .FirstOrDefault(x => x.Alias.EqualsInvariant(container.Alias) &&
                                                                  x.Type == PropertyGroupType.Tab);

            var alias = parentTab == null ? container.Alias : $"{parentTab.Alias}/{container.Alias}";

            contentType.AddPropertyGroup(alias, container.Name);

            group = contentType.PropertyGroups.First(x => x.Alias.EqualsInvariant(alias));

            if (_deterministic) {
                group.Key = UmbracoId.Deterministic(IdScope.ContentTypeContainer, Alias, alias);
            }

            var composed = contentType.ContentTypeComposition
                                      .SelectMany(x => x.PropertyGroups.OrEmpty())
                                      .FirstOrDefault(x => x.Alias.EqualsInvariant(alias));

            if (composed == null) {
                group.Type = containerType;
                group.SortOrder = sortOrder;
            } else {
                group.Type = composed.Type;
            }
        }

        var propertySortOrder = 0;

        foreach (var (propertyAlias, builder) in container.Properties) {
            ApplyProperty(contentType, group, propertyAlias, builder, propertySortOrder++);
        }
    }

    private void ApplyContainers(IContentType contentType) {
        var sortOrder = 0;

        foreach (var container in _containers) {
            ApplyContainer(contentType, container, sortOrder++);

            foreach (var child in container.Children) {
                ApplyContainer(contentType, child, sortOrder++);
            }
        }
    }

    private void ApplyProperty(IContentType contentType,
                               PropertyGroup group,
                               string propertyAlias,
                               IPropertyTypeBuilder builder,
                               int sortOrder) {
        var context = new PropertyTypeContext(Alias, propertyAlias);
        var existing = contentType.PropertyTypes.FirstOrDefault(x => x.Alias == propertyAlias);

        if (existing != null) {
            var dataType = builder.ResolveDataType(context);
            var currentContainer = contentType.PropertyGroups
                                              .FirstOrDefault(x => x.PropertyTypes.OrEmpty()
                                                                    .Any(y => y.Alias == propertyAlias));

            if (existing.DataTypeKey != dataType.Key) {
                if (existing.ValueStorageType != dataType.DatabaseType) {
                    throw new PropertyDataTypeMismatchException(Alias,
                                                               propertyAlias,
                                                               existing.ValueStorageType,
                                                               dataType.DatabaseType);
                }

                existing.DataTypeId = dataType.Id;
                existing.DataTypeKey = dataType.Key;
            }

            builder.Apply(existing, context, false);

            if (currentContainer == null && group != null) {
                contentType.MovePropertyType(propertyAlias, group.Alias);
            }
        } else if (group != null && !contentType.PropertyTypeExists(propertyAlias)) {
            var dataType = builder.ResolveDataType(context);
            var propertyType = new PropertyType(_shortStringHelper, dataType);

            propertyType.Alias = propertyAlias;
            propertyType.DataTypeKey = dataType.Key;
            propertyType.SortOrder = sortOrder;

            if (_deterministic) {
                propertyType.Key = UmbracoId.Deterministic(IdScope.PropertyType, Alias, propertyAlias);
            }

            builder.Apply(propertyType, context, true);

            contentType.AddPropertyType(propertyType, group.Alias, group.Name);
        }
    }

    private IContentType Create() {
        var contentType = new ContentType(_shortStringHelper, GetOrCreateFolder());

        contentType.Alias = Alias;

        if (_id.HasValue()) {
            contentType.Key = _id.GetValueOrThrow();
        }

        return contentType;
    }

    private IContentType FindExisting() {
        var contentType = default(IContentType);

        if (_id.HasValue()) {
            contentType = _contentTypeService.Get(_id.GetValueOrThrow());
        }

        contentType ??= _contentTypeService.Get(Alias);

        if (contentType != null && _id.HasValue() && contentType.Key != _id.GetValueOrThrow()) {
            throw new ContentTypeKeyMismatchException(Alias, _id.GetValueOrThrow(), contentType.Key);
        }

        return contentType;
    }

    private int GetOrCreateFolder() {
        var container = default(EntityContainer);
        var walkedPath = new List<string>();

        foreach (var element in _folderPath) {
            walkedPath.Add(element);

            var elementContainer = default(EntityContainer);

            if (container == null) {
                elementContainer = _contentTypeService.GetContainers(element, 1).SingleOrDefault();
            } else {
                elementContainer = _contentTypeService.GetContainers(element, container.Level + 1)
                                                      .SingleOrDefault(x => x.ParentId == container.Id);
            }

            if (elementContainer == null) {
                var key = UmbracoId.Deterministic(IdScope.ContentTypeFolder, walkedPath.ToArray());
                var attempt = _contentTypeService.CreateContainer(container?.Id ?? -1, key, element);

                if (!attempt.Success) {
                    throw new Exception($"Failed to create content type folder {element.Quote()}", attempt.Exception);
                }

                container = attempt.Result.Entity;
            } else {
                container = elementContainer;
            }
        }

        return container?.Id ?? -1;
    }
}
