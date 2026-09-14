using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using UmbracoSecurity = Umbraco.Cms.Core.Constants.Security;

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
        var contentType = FindExisting() ?? Create();
        var isNew = !contentType.HasIdentity;

        contentType.Name = _name;
        contentType.IsElement = _isElement;

        if (_icon.HasValue()) {
            contentType.Icon = _icon;
        }

        if (_description.HasValue()) {
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

        var save = isNew
                       ? _contentTypeService.CreateAsync(contentType, UmbracoSecurity.SuperUserKey)
                       : _contentTypeService.UpdateAsync(contentType, UmbracoSecurity.SuperUserKey);
        var attempt = save.GetAwaiter().GetResult();

        if (!attempt.Success) {
            throw new Exception($"Failed to save content type {_name.Quote()}: {attempt.Result}");
        }

        return contentType;
    }

    public void SetDescription(string description) {
        _description = description;
    }

    public void SetIcon(string icon) {
        _icon = icon;
    }

    public void SetName(string name) {
        _name = name;
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
            throw new Exception($"No content type found with alias {alias.Quote()}");
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
        var group = contentType.PropertyGroups.FirstOrDefault(x => x.Alias == container.Alias);

        if (group == null) {
            contentType.AddPropertyGroup(container.Alias, container.Name);

            group = contentType.PropertyGroups.First(x => x.Alias == container.Alias);

            if (_deterministic) {
                group.Key = UmbracoId.Deterministic(IdScope.ContentTypeContainer, Alias, container.Alias);
            }
        }

        group.Type = container.IsTab ? PropertyGroupType.Tab : PropertyGroupType.Group;
        group.SortOrder = sortOrder;

        var propertySortOrder = 0;

        foreach (var (propertyAlias, builder) in container.Properties) {
            ApplyProperty(contentType, container, propertyAlias, builder, propertySortOrder++);
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
                               PropertyContainerBuilder container,
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

            existing.DataTypeId = dataType.Id;
            existing.DataTypeKey = dataType.Key;
            existing.SortOrder = sortOrder;

            builder.Apply(existing, context);

            if (currentContainer?.Alias != container.Alias) {
                contentType.MovePropertyType(propertyAlias, container.Alias);
            }
        } else if (!contentType.PropertyTypeExists(propertyAlias)) {
            var dataType = builder.ResolveDataType(context);
            var propertyType = new PropertyType(_shortStringHelper, dataType);

            propertyType.Alias = propertyAlias;
            propertyType.DataTypeKey = dataType.Key;
            propertyType.SortOrder = sortOrder;

            if (_deterministic) {
                propertyType.Key = UmbracoId.Deterministic(IdScope.PropertyType, Alias, propertyAlias);
            }

            builder.Apply(propertyType, context);

            contentType.AddPropertyType(propertyType, container.Alias, container.Name);
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
