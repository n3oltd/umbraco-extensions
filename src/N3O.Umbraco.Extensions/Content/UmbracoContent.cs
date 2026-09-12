using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Lookups;
using NodaTime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.Content;

public abstract class UmbracoContent<T> : Value, IUmbracoContent {
    private IPublishedContent _content;

    // Do not use get/set property as causes issues with model validation
    public virtual IPublishedContent Content() => _content;
    
    public virtual void SetContent(IPublishedContent content) => _content = content;
    public virtual void SetVariationContext(VariationContext variationContext) => VariationContext = variationContext;

    protected VariationContext VariationContext { get; private set; }
    
    protected TProperty Child<TProperty>(Expression<Func<T, TProperty>> _)
        where TProperty : UmbracoContent<TProperty> {
        var alias = AliasHelper<TProperty>.ContentTypeAlias();
        var child = Content().Children().SingleOrDefault(x => x.ContentType.Alias.EqualsInvariant(alias));

        return child.As<TProperty>();
    }

    protected TProperty GetAs<TProperty>(Expression<Func<T, TProperty>> memberExpression) {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);
        var value = (IPublishedContent) Content().Value(alias, VariationContext?.Culture, VariationContext?.Segment);

        return value.As<TProperty>();
    }
    
    protected IEnumerable<TProperty> GetBlockListValueAs<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> memberExpression) {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);
        var values = (IEnumerable) Content().Value(alias, VariationContext?.Culture, VariationContext?.Segment)
                     ?? Enumerable.Empty<BlockListItem>();

        return values.Cast<BlockListItem>().Select(x => x.Content.As<TProperty>(_content));
    }
    
    public string GetLocalizedString<TProperty>(Expression<Func<T, TProperty>> memberExpression) {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);
        var text = (string) Content().GetProperty(alias).GetValue();
        
        return StringLocalizer.Instance.Get(GetType().GetFriendlyName(), alias, text);
    }

    protected TLookup GetLookup<TLookup>(ILookups lookups, string propertyAlias) where TLookup : ILookup {
        var propertyValue = Content().GetProperty(propertyAlias)?.GetValue();

        if (propertyValue == null) {
            return default;
        }

        TLookup lookup;
        
        if (propertyValue is TLookup lookupValue) {
            lookup = lookupValue;
        } else if (propertyValue is IPublishedContent publishedContent) {
            lookup = lookups.FindByName<TLookup>(publishedContent.Name).Single();
        } else {
            throw new Exception("Lookup properties must either be a datalist or picker");
        }

        return lookup;
    }

    protected IEnumerable<TProperty> GetNestedAs<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> memberExpression) {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);
        var values = (IEnumerable) Content().Value(alias, VariationContext?.Culture, VariationContext?.Segment)
                     ?? Enumerable.Empty<IPublishedElement>();

        return values.Cast<IPublishedElement>().Select(x => x.As<TProperty>(_content));
    }

    protected TProperty GetPickedAs<TProperty>(string alias) {
        var value = Content().Value(alias, VariationContext?.Culture, VariationContext?.Segment);

        if (value is TProperty typedValue) {
            return typedValue;
        } else {
            return ((IPublishedContent) value).As<TProperty>();
        }
    }
    
    protected TProperty GetPickedAs<TProperty>(Expression<Func<T, TProperty>> memberExpression) {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);
        var value = Content().Value(alias, VariationContext?.Culture, VariationContext?.Segment);

        if (value is TProperty typedValue) {
            return typedValue;
        } else {
            return ((IPublishedContent) value).As<TProperty>();
        }
    }
    
    protected IEnumerable<TProperty> GetPickedAs<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> memberExpression) {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);
        var values = (IEnumerable) Content().Value(alias, VariationContext?.Culture, VariationContext?.Segment)
                     ?? Enumerable.Empty<IPublishedContent>();

        return values.Cast<IPublishedContent>().Select(x => x.As<TProperty>());
    }
    
    protected TProperty GetStaticLookupByNameAs<TProperty>(Expression<Func<T, TProperty>> memberExpression)
    where TProperty : INamedLookup {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);
        var value = Content().Value<string>(alias,  VariationContext?.Culture, VariationContext?.Segment);

        return StaticLookups.GetAll<TProperty>().SingleOrDefault(x => x.Name.EqualsInvariant(value));
    }
    
    protected TProperty GetStaticLookupByIdAs<TProperty>(Expression<Func<T, TProperty>> memberExpression)
        where TProperty : INamedLookup {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);
        var value = Content().Value<string>(alias,  VariationContext?.Culture, VariationContext?.Segment);

        return StaticLookups.GetAll<TProperty>().SingleOrDefault(x => x.Id.EqualsInvariant(value));
    }

    protected LocalDate? GetLocalDate(Expression<Func<T, LocalDate?>> memberExpression) {
        return GetConvertedValue<DateTime?, LocalDate?>(memberExpression, dt => dt?.ToLocalDate());
    }
    
    protected LocalDate GetLocalDate(Expression<Func<T, LocalDate>> memberExpression) {
        return GetConvertedValue<DateTime, LocalDate>(memberExpression, dt => dt.ToLocalDate());
    }
    
    protected TProperty GetValue<TProperty>(Expression<Func<T, TProperty>> memberExpression) {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);

        var property = Content().GetProperty(alias);

        if (property == null) {
            return default;
        }

        var propertyValue = property.GetValue(VariationContext?.Culture, VariationContext?.Segment);

        if (propertyValue is TProperty typedProperty) {
            return typedProperty;
        } else if (propertyValue is IPublishedContent publishedContent) {
            return publishedContent.As<TProperty>();
        } else if (propertyValue is IPublishedElement publishedElement) {
            return publishedElement.As<TProperty>(_content);
        } else if (propertyValue is IEnumerable<BlockListItem> blockList) {
            // Nested Content handed back the element itself for a single-item property, so it matched the
            // branch above. The editor migration rewrites those properties to a Block List, which hands back
            // a BlockListModel instead, and without this the property answers null however much data it
            // holds. Nothing throws at the point of the null, so it surfaces somewhere else entirely.
            return blockList.FirstOrDefault().IfNotNull(x => x.Content.As<TProperty>(_content));
        } else {
            return default;
        }
    }
    
    protected TConverted GetConvertedValue<TProperty, TConverted>(Expression<Func<T, TConverted>> memberExpression,
                                                                  Func<TProperty, TConverted> convert) {
        var alias = AliasHelper<T>.PropertyAlias(memberExpression);

        return convert(Content().Value<TProperty>(alias,  VariationContext?.Culture, VariationContext?.Segment));
    }
}
