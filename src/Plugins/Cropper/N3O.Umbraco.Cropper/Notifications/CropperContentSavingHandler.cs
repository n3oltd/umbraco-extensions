using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Cropper.DataTypes;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cropper.Notifications;

[SkipDuringSync]
public class CropperContentSavingHandler : INotificationAsyncHandler<ContentSavingNotification> {
    private readonly IDataTypeService _dataTypeService;
    private readonly IContentHelper _contentHelper;
    private readonly IImageCropper _imageCropper;

    public CropperContentSavingHandler(IDataTypeService dataTypeService,
                                       IContentHelper contentHelper,
                                       IImageCropper imageCropper) {
        _dataTypeService = dataTypeService;
        _contentHelper = contentHelper;
        _imageCropper = imageCropper;
    }

    public async Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            var publishingCultures = content.AvailableCultures.Where(x => notification.IsSavingCulture(notification.SavedEntities.First(), x));

            if (publishingCultures.HasAny()) {
                foreach (var publishingCulture in publishingCultures) {
                    await ProcessCropperPropertiesAsync(content, publishingCulture, notification.CancelWithError, cancellationToken);
                }
            } else {
                await ProcessCropperPropertiesAsync(content, null, notification.CancelWithError, cancellationToken);
            }
        }
    }

    private IReadOnlyList<IContentProperty> GetProperties(ContentProperties content) {
        var list = new List<IContentProperty>();

        list.AddRange(content.Properties.OrEmpty());
        
        var elements = content.ElementsProperties.OrEmpty().SelectMany(x => x.Value).ToList();
            
        foreach (var element in elements) {
            list.AddRange(GetProperties(element));
        }

        return list;
    }

    private async Task GenerateCropsAsync(IContentProperty property, CancellationToken cancellationToken) {
        var dataType = _dataTypeService.GetDataType(property.Type.DataTypeId);
        var configuration = dataType.ConfigurationAs<CropperConfiguration>();
        var json = property.Value as string ?? property.Value.IfNotNull(JsonConvert.SerializeObject);
    
        if (json.HasValue()) {
            var cropperSource = JsonConvert.DeserializeObject<CropperSource>(json);

            await _imageCropper.CropAllAsync(configuration, cropperSource, property.Alias, cancellationToken);
        }
    }
    
    private async Task ProcessCropperPropertiesAsync(IContent content,
                                                     string publishingCulture,
                                                     Action<string> cancel,
                                                     CancellationToken cancellationToken) {
        var contentProperties = _contentHelper.GetContentProperties(content, publishingCulture);
        var properties = GetProperties(contentProperties);

        var cropperProperties = properties.Where(x => x.Type.HasEditorAlias(CropperConstants.PropertyEditorAlias)).ToList();

        foreach (var property in cropperProperties) {
            try {
                await GenerateCropsAsync(property, cancellationToken);
            } catch (Exception ex) {
                cancel($"Generating image crops failed with error: {ex.Message}");
            }
        }
    }
}
