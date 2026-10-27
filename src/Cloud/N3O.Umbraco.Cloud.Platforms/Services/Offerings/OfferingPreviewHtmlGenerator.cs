using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Json;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Markup;
using N3O.Umbraco.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using MediaConstants = Umbraco.Cms.Core.Constants.Conventions.Media;
using OurAllocationType = N3O.Umbraco.Giving.Allocations.Lookups.AllocationType;

namespace N3O.Umbraco.Cloud.Platforms;

public abstract class OfferingPreviewHtmlGenerator : PreviewHtmlGenerator {
    private readonly IJsonProvider _jsonProvider;
    private readonly IMediaUrl _mediaUrl;
    private readonly IMarkupEngine _markupEngine;
    private readonly IMediaLocator _mediaLocator;
    private readonly IPublishedValueFallback _publishedValueFallback;

    protected OfferingPreviewHtmlGenerator(ICdnClient cdnClient,
                                           IJsonProvider jsonProvider,
                                           IMediaUrl mediaUrl,
                                           ILookups lookups,
                                           IMarkupEngine markupEngine,
                                           IMediaLocator mediaLocator,
                                           IPublishedValueFallback publishedValueFallback,
                                           IBaseCurrencyAccessor baseCurrencyAccessor)
        : base(cdnClient, jsonProvider, lookups) {
        _jsonProvider = jsonProvider;
        _mediaUrl = mediaUrl;
        _markupEngine = markupEngine;
        _mediaLocator = mediaLocator;
        BaseCurrencyAccessor = baseCurrencyAccessor;
        _publishedValueFallback = publishedValueFallback;
    }
    
    protected abstract OurAllocationType OfferingAllocationType { get; }

    protected override string ContentTypeAlias => OfferingAllocationType.ToContentTypeAlias();

    protected override void PopulatePreviewData(IReadOnlyDictionary<string, object> content,
                                                Dictionary<string, object> previewData) {
        var image = GetMediaWithCrops(content, AliasHelper<DonationFormContentContent>.PropertyAlias(x => x.Image));
        var icon = GetMediaWithCrops(content, AliasHelper<DonationFormContentContent>.PropertyAlias(x => x.Icon));
        var shortDescription = content[AliasHelper<DonationFormContentContent>.PropertyAlias(x => x.Description)]?.ToString();
        
        var currency = BaseCurrencyAccessor.GetBaseCurrency().Code.ToEnum<Currency>();
        
        var publishedOffering = new PublishedOffering();
        publishedOffering.Id = content[AliasHelper<OfferingContent>.PropertyAlias(x => x.Key)].ToString();
        publishedOffering.Name = content[AliasHelper<IPublishedContent>.PropertyAlias(x => x.Name)]?.ToString();
        publishedOffering.FormContent = new PublishedDonationFormContent();
        publishedOffering.FormContent.Image = new PublishedImageContent();
        publishedOffering.FormContent.Image.Format = PropertyFormat.Image;
        publishedOffering.FormContent.Image.Main = new PublishedProcessedImage();
        publishedOffering.FormContent.Image.Main.Url = new Uri(_mediaUrl.GetMediaUrl(image, urlMode: UrlMode.Absolute));
        publishedOffering.FormContent.Image.Main.Size = new PublishedSize();
        publishedOffering.FormContent.Image.Main.Size.Width = (int) image.Properties.Single(x => x.Alias == MediaConstants.Width).GetValue();
        publishedOffering.FormContent.Image.Main.Size.Height = (int) image.Properties.Single(x => x.Alias == MediaConstants.Height).GetValue();
        
        publishedOffering.FormContent.Icon = new PublishedSvgContent();
        publishedOffering.FormContent.Icon.Url = _mediaUrl.GetMediaUrl(icon, urlMode: UrlMode.Absolute).IfNotNull(x => new Uri(x));
        publishedOffering.FormContent.Icon.Format = PropertyFormat.Svg;
        
        publishedOffering.FormContent.Description = new PublishedHtmlContent();
        publishedOffering.FormContent.Description.Markup = _markupEngine.RenderHtml(shortDescription).IfNotNull(x => new HtmlEncodedString(x.ToString())).ToHtmlString();
        publishedOffering.FormContent.Description.Format = PropertyFormat.Html;
        
        publishedOffering.FormState = new PublishedDonationFormState();
        publishedOffering.FormState.CartItem = new PublishedCartItem();
        publishedOffering.FormState.CartItem.Type = CartItemType.NewDonation;
        publishedOffering.FormState.CartItem.Currency = currency;
        publishedOffering.FormState.CartItem.Value = new MoneyRes();
        publishedOffering.FormState.CartItem.Value.Currency = currency;
        publishedOffering.FormState.CartItem.Value.Amount = 0d;
        
        publishedOffering.FormState.CartItem.Type = CartItemType.NewDonation;

        publishedOffering.FormState.CartItem.NewDonation = new PublishedNewDonation();
        publishedOffering.FormState.CartItem.NewDonation.Allocation = new PublishedAllocationIntent();
        publishedOffering.FormState.CartItem.NewDonation.Allocation.FundDimensions = GetPublishedOfferingFundDimensions(content);
        publishedOffering.FormState.CartItem.NewDonation.Allocation.Value = new Money();
        publishedOffering.FormState.CartItem.NewDonation.Allocation.Value.Currency = currency;
        publishedOffering.FormState.CartItem.NewDonation.Allocation.Value.Amount = 0d;
        
        PopulateAllocationIntent(content, publishedOffering.FormState.CartItem.NewDonation.Allocation);
        
        publishedOffering.FormState.Options = new PublishedDonationFormOptions();
        PopulateFormStateOptions(content, publishedOffering.FormState.Options);
        
        var publishedDonationForm = new PublishedDonationForm();
        publishedDonationForm.Id = content[AliasHelper<OfferingContent>.PropertyAlias(x => x.Key)].ToString();
        publishedDonationForm.FormState = publishedOffering.FormState;

        previewData["element"] = publishedDonationForm;
        previewData["offering"] = publishedOffering;
        
        PopulateAdditionalData(previewData, publishedDonationForm);
    }

    private PublishedFundDimensionValues GetPublishedOfferingFundDimensions(IReadOnlyDictionary<string, object> content) {
        var dimension1 = GetDataListValue<FundDimension1Value>(content, AliasHelper<DonationFormStateContent>.PropertyAlias(x => x.Dimension1));
        var dimension2 = GetDataListValue<FundDimension2Value>(content, AliasHelper<DonationFormStateContent>.PropertyAlias(x => x.Dimension2));
        var dimension3 = GetDataListValue<FundDimension3Value>(content, AliasHelper<DonationFormStateContent>.PropertyAlias(x => x.Dimension3));
        var dimension4 = GetDataListValue<FundDimension4Value>(content, AliasHelper<DonationFormStateContent>.PropertyAlias(x => x.Dimension4));
        
        var fundDimensionOptions = GetFundDimensionOptions(content);
        
        var publishedFundDimensions = new PublishedFundDimensionValues();
        publishedFundDimensions.Dimension1 = dimension1?.Name ?? (fundDimensionOptions.Dimension1.IsSingle() ? fundDimensionOptions.Dimension1.Single()?.Name : null);
        publishedFundDimensions.Dimension2 = dimension2?.Name ?? (fundDimensionOptions.Dimension2.IsSingle() ? fundDimensionOptions.Dimension2.Single()?.Name : null);
        publishedFundDimensions.Dimension3 = dimension3?.Name ?? (fundDimensionOptions.Dimension3.IsSingle() ? fundDimensionOptions.Dimension3.Single()?.Name : null);
        publishedFundDimensions.Dimension4 = dimension4?.Name ?? (fundDimensionOptions.Dimension4.IsSingle() ? fundDimensionOptions.Dimension4.Single()?.Name : null);

        return publishedFundDimensions;
    }

    private MediaWithCrops GetMediaWithCrops(IReadOnlyDictionary<string, object> content, string alias) {
        var mediaDto = _jsonProvider.DeserializeObject<IEnumerable<MediaWithCropsDto>>(content[alias]?.ToString()).FirstOrDefault();

        if (!mediaDto.HasValue()) {
            return null;
        }

        return mediaDto?.ToMediaWithCrops(_publishedValueFallback, _mediaLocator);
    }
    
    protected IBaseCurrencyAccessor BaseCurrencyAccessor { get; set; }
    
    protected abstract void PopulateAllocationIntent(IReadOnlyDictionary<string, object> content,
                                                     PublishedAllocationIntent publishedOffering);
    
    protected virtual void PopulateFormStateOptions(IReadOnlyDictionary<string, object> content,
                                                    PublishedDonationFormOptions publishedOffering) { }
    
    
    protected abstract IFundDimensionOptions GetFundDimensionOptions(IReadOnlyDictionary<string, object> content);

    protected virtual void PopulateAdditionalData(Dictionary<string, object> previewData,
                                                  PublishedDonationForm publishedDonationForm) { }
}