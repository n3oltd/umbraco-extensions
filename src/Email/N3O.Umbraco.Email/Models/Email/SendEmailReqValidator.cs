using FluentValidation;
using Humanizer.Bytes;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Validation;
using System;
using System.Linq;

namespace N3O.Umbraco.Email.Models;

public class SendEmailReqValidator : ModelValidator<SendEmailReq> {
    private const int MaximumSubjectLength = 150;
    private const int MaximumBodyLength = 150_000;
    private static readonly ByteSize MaximumAttachmentsSize = ByteSize.FromMegabytes(10);

    public SendEmailReqValidator(IFormatter formatter) : base(formatter) {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage(Get<Strings>(s => s.SpecifySubject));
        
        RuleFor(x => x.Subject)
            .MaximumLength(MaximumSubjectLength)
            .When(x => x.Subject.HasValue())
            .WithMessage(Get<Strings>(s => s.SubjectTooLong_1, MaximumSubjectLength));
        
        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage(Get<Strings>(s => s.SpecifyBody));
        
        RuleFor(x => x.Body)
            .MaximumLength(MaximumBodyLength)
            .When(x => x.Body.HasValue())
            .WithMessage(Get<Strings>(s => s.BodyTooLong_1, MaximumBodyLength));
        
        RuleFor(x => x)
            .Must(m => m.To.HasAny() || m.Cc.HasAny() || m.Bcc.HasAny())
            .WithMessage(Get<Strings>(s => s.NoRecipients));

        RuleFor(x => x)
            .Must(m => GetAttachmentsSize(m) <= MaximumAttachmentsSize)
            .When(x => x.Attachments.HasAny())
            .WithMessage(x => Get<Strings>(s => s.AttachmentsTooLarge_2,
                                           Math.Round(GetAttachmentsSize(x).Megabytes, 2),
                                           Math.Round(MaximumAttachmentsSize.Megabytes, 2)));
    }

    private static ByteSize GetAttachmentsSize(SendEmailReq req) {
        return ByteSize.FromBytes(req.Attachments.OrEmpty().Sum(x => x.Size.Bytes));
    }

    public class Strings : ValidationStrings {
        public string AttachmentsTooLarge_2 => "The attached files are {0}MB which exceeds the maximum allowed size of {1}MB";
        public string BodyTooLong_1 => "Body length exceeds the allowed maximum of {0}";
        public string NoRecipients => "No recipients have been specified";
        public string SpecifyBody => "Body must be specified";
        public string SpecifySubject => "Subject must be specified";
        public string SubjectTooLong_1 => "Subject length exceeds the allowed maximum of {0}";
    }
}
