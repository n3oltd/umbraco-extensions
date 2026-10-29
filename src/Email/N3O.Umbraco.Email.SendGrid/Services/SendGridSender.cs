using MimeKit;
using N3O.Umbraco.Email.SendGrid.Extensions;
using SendGrid;
using System;
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

    public async Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default) {
        var sendGridMessage = message.ToSendGridMessage();
        sendGridMessage.SetSandBoxMode(_sandboxMode);
        sendGridMessage.AddCategory(SendGridConstants.Category);

        var response = await _sendGridClient.SendEmailAsync(sendGridMessage, cancellationToken);

        if (!response.IsSuccessStatusCode) {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);

            throw new Exception($"SendGrid responded with {(int) response.StatusCode} {response.StatusCode}: {body}");
        }

        var messageId = response.Headers.TryGetValues("X-Message-ID", out var messageIds)
                            ? messageIds.First()
                            : null;

        return messageId;
    }
}
