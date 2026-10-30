using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Extensions;
using OurDataType = N3O.Umbraco.Data.Lookups.DataType;
using OurDataTypes = N3O.Umbraco.Data.Lookups.DataTypes;

namespace N3O.Umbraco.Data.Parsing;

public class PublishedContentParser : DataTypeParser<IPublishedContent>, IPublishedContentParser {
    private readonly IPublishedContentCache _publishedContentCache;
    private readonly IDocumentNavigationQueryService _navigationQueryService;

    public PublishedContentParser(IPublishedContentCache publishedContentCache,
                                  IDocumentNavigationQueryService navigationQueryService) {
        _publishedContentCache = publishedContentCache;
        _navigationQueryService = navigationQueryService;
    }

    public override bool CanParse(OurDataType dataType) {
        return dataType == OurDataTypes.Content;
    }

    public override ParseResult<IPublishedContent> Parse(string text, Type targetType) {
        return Parse(text, targetType, null);
    }

    public ParseResult<IPublishedContent> Parse(string text, Type targetType, Guid? parentId) {
        IPublishedContent value = null;

        if (text.HasValue()) {
            text = text.Trim();

            if (Guid.TryParse(text, out var id)) {
                value = _publishedContentCache.GetById(id);
            } else {
                var searchRoots = new List<IPublishedContent>();

                if (parentId != null) {
                    var parent = _publishedContentCache.GetById(parentId.Value);

                    if (parent.HasValue()) {
                        searchRoots.Add(parent);
                    }
                } else {
                    var rootContents = _navigationQueryService.GetPublishedRootContents(_publishedContentCache);
                    
                    searchRoots.AddRange(rootContents);
                }

                IReadOnlyList<IPublishedContent> matches;

                if (text.Contains(DataConstants.Separator)) {
                    matches = PathSearch(searchRoots, text);
                } else {
                    matches = NameSearch(searchRoots, text);
                }

                if (matches.IsSingle()) {
                    value = matches.Single();
                }
            }

            if (value == null) {
                return ParseResult.Fail<IPublishedContent>();
            }
        }

        return ParseResult.Success(value);
    }

    public ParseResult<IPublishedContent> Parse(JToken token, Type targetType, Guid? parentId) {
        if (token.Type == JTokenType.String) {
            return Parse((string) token, targetType, parentId);
        } else {
            return ParseResult.Fail<IPublishedContent>();
        }
    }

    private IReadOnlyList<IPublishedContent> PathSearch(IReadOnlyList<IPublishedContent> searchRoots, string text) {
        var path = text.Split(DataConstants.Separator,
                              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return searchRoots.SelectMany(r => r.Descendants().Select(x => new { Content = x, Path = GetPath(r, x)}))
                          .Where(x => PathsMatch(path, x.Path))
                          .Select(x => x.Content)
                          .ToList();
    }

    private IReadOnlyList<IPublishedContent> NameSearch(IReadOnlyList<IPublishedContent> searchRoots, string text) {
        return searchRoots.SelectMany(r => r.Descendants().Where(c => c.Name.EqualsInvariant(text))).ToList();
    }

    private IReadOnlyList<string> GetPath(IPublishedContent root, IPublishedContent content) {
        var path = new List<string>();

        while (true) {
            path.Add(content.Name);

            content = content.Parent();

            if (content == root) {
                break;
            }
        }

        path.Reverse();

        return path;
    }

    private bool PathsMatch(IReadOnlyList<string> path1, IReadOnlyList<string> path2) {
        if (path1.Count != path2.Count) {
            return false;
        }

        for (var i = 0; i < path1.Count; i++) {
            if (!path1[i].EqualsInvariant(path2[i])) {
                return false;
            }
        }

        return true;
    }
}
