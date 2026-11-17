using HandlebarsDotNet;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Templates.Handlebars;

public class HandlebarsCompiler : IHandlebarsCompiler {
    private readonly IMemoryCache _cache;
    private readonly IHandlebarsFactory _handlebarsFactory;

    public HandlebarsCompiler(IMemoryCache cache, IHandlebarsFactory handlebarsFactory) {
        _cache = cache;
        _handlebarsFactory = handlebarsFactory;
    }

    public bool IsSyntaxValid(string content) {
        try {
            var handlebars = GetHandlebars(null, false);
        
            handlebars.Compile(content);

            return true;
        } catch {
            return false;
        }
    }

    public HandlebarsTemplate<object, object> Compile(string markup,
                                                      IReadOnlyDictionary<string, string> partials = null,
                                                      string cacheKey = null,
                                                      bool noEscape = false) {
        HandlebarsTemplate<object, object> compiled;

        if (cacheKey != null) {
            compiled = _cache.GetOrCreate(cacheKey, c => {
                var result = DoCompile(markup, partials, noEscape);

                c.SlidingExpiration = TimeSpan.FromHours(1);

                return result;
            });
        } else {
            compiled = DoCompile(markup, partials, noEscape);
        }

        return compiled;
    }

    private HandlebarsTemplate<object, object> DoCompile(string markup,
                                                         IReadOnlyDictionary<string, string> partials,
                                                         bool noEscape) {
        var handlebars = GetHandlebars(partials, noEscape);
        
        var compiledTemplate = handlebars.Compile(markup);

        return compiledTemplate;
    }

    private IHandlebars GetHandlebars(IReadOnlyDictionary<string, string> partials, bool noEscape) {
        var handlebars = _handlebarsFactory.Create(partials, noEscape);

        return handlebars;
    }
}
