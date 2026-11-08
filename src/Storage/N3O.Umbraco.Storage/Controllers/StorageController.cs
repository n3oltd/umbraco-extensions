using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Storage.Models;
using N3O.Umbraco.Validation;
using N3O.Umbraco.Validation.Controllers;
using NodaTime;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Umbraco.Cms.Web.Common.Authorization;

namespace N3O.Umbraco.Storage.Controllers;

[ApiDocument(StorageConstants.ApiName)]
public class StorageController : ValidatingApiController {
    private readonly IClock _clock;
    private readonly IVolume _volume;

    public StorageController(IClock clock,
                             IVolume volume,
                             IValidation validation,
                             Lazy<IValidationHandler> validationHandler) 
        : base(validation, validationHandler) {
        _clock = clock;
        _volume = volume;
    }
    
    [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
    [HttpPost("download/{folderName}/{filename}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Download(string folderName, string filename) {
        try {
            var storageFolder = await _volume.GetStorageFolderAsync(folderName);
            var blob = await storageFolder.GetFileAsync(filename);

            return File(blob.Stream, blob.ContentType, blob.Filename);
        } catch {
            return NotFound();
        }
    }
    
    [HttpPost("tempUpload")]
    [RequestSizeLimit(1024_000_000)]
    public async Task<ActionResult<StorageToken>> TempUpload([FromForm] UploadReq req) {
        await ValidateAsync(req);
        
        var folderPath = Path.Join(StorageConstants.StorageFolders.Temp,
                                   $"_{_clock.GetCurrentInstant().ToUnixTimeTicks()}");
        
        return await Upload(folderPath, req);
    }

    private async Task<ActionResult<StorageToken>> Upload(string folderPath, [FromForm] UploadReq req) {
        await ValidateAsync(req);
        
        using (var reqStream = req.File.OpenReadStream()) {
            var filename = Sanitise(req.File.FileName);
            var storageFolder = await _volume.GetStorageFolderAsync(folderPath);
            await storageFolder.AddFileAsync(filename, reqStream);

            var blob = await storageFolder.GetFileAsync(filename);

            using (blob.Stream) {
                return Ok(StorageToken.FromBlob(blob));
            }
        }
    }

    private string Sanitise(string filename) {
        return Regex.Replace(filename,
                             @"[^a-z0-9\-_\.]",
                             "",
                             RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
