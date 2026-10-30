using N3O.Umbraco.Content;
using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using OurDataType = N3O.Umbraco.Data.Lookups.DataType;
using OurDataTypes = N3O.Umbraco.Data.Lookups.DataTypes;

namespace N3O.Umbraco.Data.Parsing;

public class ContentParser : DataTypeParser<IContent>, IContentParser {
    private readonly IContentService _contentService;
    private readonly IContentHelper _contentHelper;

    public ContentParser(IContentService contentService,
                         IContentHelper contentHelper) {
        _contentService = contentService;
        _contentHelper = contentHelper;
    }
    
    public override bool CanParse(OurDataType dataType) {
        return dataType == OurDataTypes.Content;
    }

    public override ParseResult<IContent> Parse(string text, Type targetType) {
        return Parse(text, targetType, null);
    }

    public ParseResult<IContent> Parse(string text, Type targetType, Guid? parentId) {
        IContent value = null;
        
        if (text.HasValue()) {
                text = text.Trim();
                
                if (Guid.TryParse(text, out var id)) {
                    value = _contentService.GetById(id);
                } else {
                    var searchRoots = new List<IContent>();

                    if (parentId != null) {
                        var parent = _contentService.GetById(parentId.Value);

                        if (parent.HasValue()) {
                            searchRoots.Add(parent);
                        }
                    } else {
                        searchRoots.AddRange(_contentService.GetRootContent());
                    }

                    var matches = searchRoots.SelectMany(r => _contentHelper.GetDescendants(r)
                                                                            .Where(c => c.Name.EqualsInvariant(text)))
                                             .ToList();

                    if (matches.IsSingle()) {
                        value = matches.Single();
                    }
                }

                if (value == null) {
                    return ParseResult.Fail<IContent>();
                }
        }
        
        return ParseResult.Success(value);
    }

    public ParseResult<IContent> Parse(JToken token, Type targetType, Guid? parentId) {
        if (token.Type == JTokenType.String) {
            return Parse((string) token, targetType, parentId);
        } else {
            return ParseResult.Fail<IContent>();
        }
    }
}
