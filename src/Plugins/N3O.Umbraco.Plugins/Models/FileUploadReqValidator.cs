using FluentValidation;
using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Plugins.Lookups;
using N3O.Umbraco.Validation;
using System;
using System.Linq;

namespace N3O.Umbraco.Plugins.Models;

public class FileUploadReqValidator : ModelValidator<FileUploadReq> {
    public FileUploadReqValidator(IFormatter formatter) : base(formatter) {
        RuleFor(x => x.File)
           .NotNull()
           .WithMessage(Get<Strings>(s => s.SpecifyFile));

        RuleFor(x => x.File)
           .Must((req, x) => ValidateAllowedExtensions(req.AllowedExtensions, x))
           .When(x => x.File.HasValue() && x.AllowedExtensions.HasValue())
           .WithMessage(Get<Strings>(s => s.InvalidExtension));
        
        RuleFor(x => x.File)
           .Must((req, x) => ValidateImageOnly(req.ImagesOnly, x))
           .When(x => x.File.HasValue())
           .WithMessage(Get<Strings>(s => s.InvalidImage));
    }

    private bool ValidateImageOnly(bool? imagesOnly, IFormFile formFile) {
        if (imagesOnly == true) {
            var allowedFormats = ImageFormats.GetAllFormats().ToList();
            
            return allowedFormats.Any(x => formFile.FileName.EndsWith(x.Id));
        }

        return true;
    }

    private bool ValidateAllowedExtensions(string allowedExtensions, IFormFile fileUpload) {
        return allowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Any(fileUpload.FileName.EndsWith);
    }
    
    public class Strings : ValidationStrings {
        public string InvalidExtension => "The specified file has invalid extension";
        public string InvalidImage => "The specified file is not an image";
        public string SpecifyFile => "Please specify the file";
    }
}