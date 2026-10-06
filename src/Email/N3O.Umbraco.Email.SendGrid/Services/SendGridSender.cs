using N3O.Umbraco.Email.Models;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email.SendGrid;

public class SendGridSender : IEmailSender {
    private readonly bool _sandboxMode;
    private readonly SendGridClient _sendGridClient;

    public SendGridSender(string apiKey, bool sandboxMode) {
        _sandboxMode = sandboxMode;
        _sendGridClient = new SendGridClient(apiKey);
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken) {
        var sendGridMessage = BuildSendGridMessage(message);

        var response = await _sendGridClient.SendEmailAsync(sendGridMessage, cancellationToken);

        if (response.IsSuccessStatusCode) {
            return new EmailSendResult([]);
        }

        var errors = await GetErrorsAsync(response);

        return new EmailSendResult(errors);
    }

    private SendGridMessage BuildSendGridMessage(EmailMessage message) {
        var sendGridMessage = new SendGridMessage();
        sendGridMessage.SetSandBoxMode(_sandboxMode);
        sendGridMessage.SetFrom(ToEmailAddress(message.From));

        if (message.To.Any()) {
            sendGridMessage.AddTos(message.To.Select(ToEmailAddress).ToList());
        }

        if (message.Cc.Any()) {
            sendGridMessage.AddCcs(message.Cc.Select(ToEmailAddress).ToList());
        }

        if (message.Bcc.Any()) {
            sendGridMessage.AddBccs(message.Bcc.Select(ToEmailAddress).ToList());
        }

        sendGridMessage.SetSubject(message.Subject);
        sendGridMessage.HtmlContent = message.HtmlBody;
        sendGridMessage.AddCategory(message.Tag);

        foreach (var attachment in message.Attachments) {
            sendGridMessage.AddAttachment(attachment.Name, Convert.ToBase64String(attachment.Bytes), attachment.ContentType);
        }

        return sendGridMessage;
    }

    private async Task<IReadOnlyList<string>> GetErrorsAsync(Response response) {
        var errors = new List<string>();
        errors.Add($"{response.StatusCode}");

        var body = await response.DeserializeResponseBodyAsync();

        if (body.TryGetValue("errors", out var bodyErrors)) {
            foreach (var bodyError in bodyErrors) {
                errors.Add($"{bodyError}");
            }
        }

        return errors;
    }

    private EmailAddress ToEmailAddress(EmailIdentity emailIdentity) {
        return new EmailAddress(emailIdentity.Email, emailIdentity.Name);
    }
}
