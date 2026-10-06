using N3O.Umbraco.Email.Commands;
using N3O.Umbraco.Email.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email.Handlers;

public class SendEmailHandler : IRequestHandler<SendEmailCommand, SendEmailReq, None> {
    private readonly IJsonProvider _jsonProvider;
    private readonly ITemplateRenderer _renderer;
    private readonly IEmailSender _sender;

    public SendEmailHandler(IJsonProvider jsonProvider, ITemplateRenderer renderer, IEmailSender sender) {
        _jsonProvider = jsonProvider;
        _renderer = renderer;
        _sender = sender;
    }

    public async Task<None> Handle(SendEmailCommand req, CancellationToken cancellationToken) {
        var templateModel = _jsonProvider.DeserializeObject(req.Model.ModelJson, TypeResolver.Resolve(req.Model.ModelType));
        var subject = await _renderer.ParseAsync(req.Model.Subject, templateModel, false);
        var htmlBody = _renderer.Parse(req.Model.Body, templateModel);

        var message = new EmailMessage(new EmailIdentity(req.Model.From),
                                       ToEmailIdentities(req.Model.To),
                                       ToEmailIdentities(req.Model.Cc),
                                       ToEmailIdentities(req.Model.Bcc),
                                       subject,
                                       htmlBody,
                                       ToEmailAttachments(req.Model.Attachments),
                                       "website");

        var result = await _sender.SendAsync(message, cancellationToken);

        if (!result.Success) {
            var errorMessage = $"Error occured while sending email to {message.To.First().Name}";

            foreach (var error in result.Errors) {
                errorMessage += $"\n{error}";
            }

            throw new Exception(errorMessage);
        }

        return None.Empty;
    }

    private IReadOnlyList<EmailAttachment> ToEmailAttachments(IEnumerable<EmailAttachmentReq> attachments) {
        return attachments.OrEmpty().Select(x => new EmailAttachment(x.Name, x.ContentType, x.Bytes)).ToList();
    }

    private IReadOnlyList<EmailIdentity> ToEmailIdentities(IEnumerable<IEmailIdentity> recipients) {
        return recipients.OrEmpty().Select(x => new EmailIdentity(x)).ToList();
    }
}
