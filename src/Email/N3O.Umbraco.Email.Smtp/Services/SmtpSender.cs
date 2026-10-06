using MailKit.Net.Smtp;
using MailKit.Security;
using N3O.Umbraco.Email.Lookups;
using N3O.Umbraco.Email.Models;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email.Smtp;

public class SmtpSender : IEmailSender {
    private readonly IMimeMessageBuilder _mimeMessageBuilder;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public SmtpSender(IMimeMessageBuilder mimeMessageBuilder, string host, int port, string username, string password) {
        _mimeMessageBuilder = mimeMessageBuilder;
        _host = host;
        _port = port;
        _username = username;
        _password = password;
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
            await smtpClient.AuthenticateAsync(_username, _password, cancellationToken);
            await smtpClient.SendAsync(mimeMessage, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);
        }

        return new EmailSendResult([]);
    }
}
