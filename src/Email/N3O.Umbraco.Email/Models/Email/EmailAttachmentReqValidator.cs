using FluentValidation;
using MimeKit;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Validation;

namespace N3O.Umbraco.Email.Models;

public class EmailAttachmentReqValidator : ModelValidator<EmailAttachmentReq> {
    public EmailAttachmentReqValidator(IFormatter formatter) : base(formatter) {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(Get<Strings>(s => s.SpecifyName));

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage(Get<Strings>(s => s.SpecifyContentType));

        RuleFor(x => x.ContentType)
            .Must(x => ContentType.TryParse(x, out _))
            .When(x => x.ContentType.HasValue())
            .WithMessage(Get<Strings>(s => s.ContentTypeInvalid));

        RuleFor(x => x.Size)
            .Must(x => x.Bytes > 0)
            .WithMessage(Get<Strings>(s => s.SizeInvalid));
    }

    public class Strings : ValidationStrings {
        public string ContentTypeInvalid => "Content type must be in the form type/subtype";
        public string SizeInvalid => "Size must be greater than zero";
        public string SpecifyContentType => "Content type must be specified";
        public string SpecifyName => "Name must be specified";
    }
}
