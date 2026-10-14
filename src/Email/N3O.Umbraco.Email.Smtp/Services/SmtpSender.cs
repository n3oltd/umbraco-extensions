using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using N3O.Umbraco.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email.Smtp;

public class SmtpSender : IEmailSender {
    private readonly string _host;
    private readonly ILogger<SmtpSender> _logger;
    private readonly string _password;
    private readonly int _port;
    private readonly string _username;

    public SmtpSender(ILogger<SmtpSender> logger, string host, int port, string username, string password) {
        _host = host;
        _logger = logger;
        _password = password;
        _port = port;
        _username = username;
    }

    public async Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default) {
        using (var smtpClient = new SmtpClient()) {
            using (var timeout = CreateTimeout(ConnectTimeout, cancellationToken)) {
                await smtpClient.ConnectAsync(_host, _port, GetSecureSocketOptions(), timeout.Token);
            }

            if (_username.HasValue() && _password.HasValue()) {
                using (var timeout = CreateTimeout(AuthenticateTimeout, cancellationToken)) {
                    await smtpClient.AuthenticateAsync(_username, _password, timeout.Token);
                }
            }

            using (var timeout = CreateTimeout(SendTimeout, cancellationToken)) {
                await smtpClient.SendAsync(message, timeout.Token);
            }

            await DisconnectAsync(smtpClient);
        }

        return message.MessageId;
    }

    private static CancellationTokenSource CreateTimeout(TimeSpan timeout, CancellationToken cancellationToken) {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(timeout);

        return cancellationTokenSource;
    }

    private async Task DisconnectAsync(SmtpClient smtpClient) {
        try {
            using (var timeout = CreateTimeout(DisconnectTimeout, CancellationToken.None)) {
                await smtpClient.DisconnectAsync(true, timeout.Token);
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to disconnect from SMTP server {Host} after sending", _host);
        }
    }

    private SecureSocketOptions GetSecureSocketOptions() {
        return _port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }

    private static readonly TimeSpan AuthenticateTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);
}
