using HandlebarsDotNet;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Templates.Handlebars.BlockHelpers;
using N3O.Umbraco.Templates.Handlebars.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Templates.Handlebars;

public class HandlebarsFactory : IHandlebarsFactory {
    private readonly IReadOnlyList<IHelper> _helpers;
    private readonly IReadOnlyList<IBlockHelper> _blockHelpers;
    private readonly IEnumerable<ITemplateFormatter> _templateFormatters;

    public HandlebarsFactory(IEnumerable<IHelper> helpers,
                             IEnumerable<IBlockHelper> blockHelpers,
                             IEnumerable<ITemplateFormatter> templateFormatters) {
        _helpers = helpers.ToList();
        _blockHelpers = blockHelpers.ToList();
        _templateFormatters = templateFormatters;
    }

    public IHandlebars Create(IReadOnlyDictionary<string, string> partials = null, bool noEscape = false) {
        var handlebars = HandlebarsDotNet.Handlebars.Create();
        handlebars.Configuration.NoEscape = noEscape;
    
        _helpers.Do(h => handlebars.RegisterHelper(h.Name, h.Execute));
    
        _blockHelpers.Do(b => handlebars.RegisterHelper(b.Name, b.Execute));
    
        _templateFormatters.Do(f => handlebars.Configuration.FormatterProviders.Add(new HandlebarsFormatter(f)));
        
        foreach (var (partialAlias, partialMarkup) in partials.OrEmpty()) {
            handlebars.RegisterTemplate(partialAlias, partialMarkup);
        }

        return handlebars;
    }
}
