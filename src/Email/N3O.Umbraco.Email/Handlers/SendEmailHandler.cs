using Microsoft.Extensions.Logging;
using N3O.Umbraco.Email.Commands;
using N3O.Umbraco.Email.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Storage;
using N3O.Umbraco.Templates;
using N3O.Umbraco.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email.Handlers;

public class SendEmailHandler : IRequestHandler<SendEmailCommand, SendEmailReq, None> {
    private readonly IJsonProvider _jsonProvider;
    private readonly ILogger<SendEmailHandler> _logger;
    private readonly IMimeMessageBuilder _mimeMessageBuilder;
    private readonly IEmailSender _sender;
    private readonly ITemplateEngine _templateEngine;
    private readonly IVolume _volume;

    public SendEmailHandler(IJsonProvider jsonProvider,
                            ILogger<SendEmailHandler> logger,
                            IMimeMessageBuilder mimeMessageBuilder,
                            IEmailSender sender,
                            ITemplateEngine templateEngine,
                            IVolume volume) {
        _jsonProvider = jsonProvider;
        _logger = logger;
        _mimeMessageBuilder = mimeMessageBuilder;
        _sender = sender;
        _templateEngine = templateEngine;
        _volume = volume;
    }

    public async Task<None> Handle(SendEmailCommand req, CancellationToken cancellationToken) {
        var templateModel = _jsonProvider.DeserializeObject(req.Model.ModelJson, TypeResolver.Resolve(req.Model.ModelType));
        var subject = _templateEngine.Render(req.Model.Subject, templateModel);
        var htmlBody = _templateEngine.Render(req.Model.Body, templateModel);

        var to = ToEmailIdentities(req.Model.To);
        var cc = ToEmailIdentities(req.Model.Cc);
        var bcc = ToEmailIdentities(req.Model.Bcc);
        var recipient = to.Concat(cc).Concat(bcc).First();

        var mimeMessage = await _mimeMessageBuilder.BuildMessageAsync(new EmailIdentity(req.Model.From),
                                                                      to,
                                                                      cc,
                                                                      bcc,
                                                                      subject,
                                                                      htmlBody,
                                                                      req.Model.Attachments,
                                                                      cancellationToken);

        string messageId;

        try {
            messageId = await _sender.SendAsync(mimeMessage, cancellationToken);
        } catch (Exception ex) when (ex is not OperationCanceledException ||
                                     !cancellationToken.IsCancellationRequested) {
            throw new Exception($"Error occurred while sending email to {recipient}", ex);
        }

        _logger.LogInformation("Sent email to {Recipient} with provider message ID {MessageId}",
                               recipient,
                               messageId);

        await DeleteAttachmentsAsync(req.Model.Attachments);

        return None.Empty;
    }

    private async Task DeleteAttachmentsAsync(IEnumerable<EmailAttachmentReq> attachments) {
        foreach (var storageFolderName in attachments.OrEmpty().Select(x => x.StorageFolderName).Distinct()) {
            try {
                var storageFolder = await _volume.GetStorageFolderAsync(storageFolderName);

                await storageFolder.DeleteAllFilesAsync();
            } catch (Exception ex) {
                // A retry would send the email again, so a failed delete must not fail the job.
                _logger.LogWarning(ex,
                                   "Failed to delete the attachments in {StorageFolderName} after sending",
                                   storageFolderName);
            }
        }
    }

    private IReadOnlyList<EmailIdentity> ToEmailIdentities(IEnumerable<IEmailIdentity> recipients) {
        return recipients.OrEmpty().Select(x => new EmailIdentity(x)).ToList();
    }
}
