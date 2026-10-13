using MailKit.Net.Smtp;
using MailKit.Security;
using N3O.Umbraco.Email.Lookups;
using N3O.Umbraco.Email.Models;
using N3O.Umbraco.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email.Smtp;

public class SmtpSender : IEmailSender {
    private readonly string _host;
    private readonly IMimeMessageBuilder _mimeMessageBuilder;
    private readonly string _password;
    private readonly int _port;
    private readonly string _username;

    public SmtpSender(IMimeMessageBuilder mimeMessageBuilder, string host, int port, string username, string password) {
        _host = host;
        _mimeMessageBuilder = mimeMessageBuilder;
        _password = password;
        _port = port;
        _username = username;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken) {
        var mimeMessage = _mimeMessageBuilder.BuildMessage(message.From,
                                                           message.To,
                                                           message.Cc,
                                                           message.Bcc,
                                                           message.Subject,
                                                           message.HtmlBody,
                                                           BodyFormats.Html,
                                                           message.Attachments);

        using (var smtpClient = new SmtpClient()) {
            await smtpClient.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, cancellationToken);

            if (_username.HasValue() && _password.HasValue()) {
                await smtpClient.AuthenticateAsync(_username, _password, cancellationToken);
            }

            await smtpClient.SendAsync(mimeMessage, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);
        }

        return EmailSendResult.ForSuccess();
    }
}
