using System;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.ContentTypes;

public interface IContentTypeDesigner {
    void AddComposition(string contentTypeAlias);
    void AddComposition<T>();
    IPropertyContainerBuilder Group(string name);
    void InFolder(params string[] path);
    IContentType Save();
    void SetDescription(string description, bool overwriteExisting = false);
    void SetIcon(string icon, bool overwriteExisting = false);
    void SetName(string name, bool overwriteExisting = false);
    IPropertyContainerBuilder Tab(string name);
    void VaryByCulture();
    void VaryBySegment();
    void WithDeterministicId();
    void WithId(Guid id);
}

public interface IContentTypeDesigner<T> : IContentTypeDesigner {
    new IPropertyContainerBuilder<T> Group(string name);
    new IPropertyContainerBuilder<T> Tab(string name);
}
