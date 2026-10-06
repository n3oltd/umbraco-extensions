using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using MimeKit;
using N3O.Umbraco.Email.Extensions;
using N3O.Umbraco.Email.Lookups;
using N3O.Umbraco.Email.Models;
using N3O.Umbraco.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email.Amazon;

public class AmazonSender : IEmailSender {
    private readonly IMimeMessageBuilder _mimeMessageBuilder;
    private readonly AmazonSimpleEmailServiceClient _sesClient;

    public AmazonSender(IMimeMessageBuilder mimeMessageBuilder, string accessKey, string secretKey, string regionCode) {
        _mimeMessageBuilder = mimeMessageBuilder;

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var regionEndpoint = typeof(RegionEndpoint).GetConstantOrStaticValues<RegionEndpoint>()
                                                   .SingleOrDefault(x => x.SystemName.EqualsInvariant(regionCode));

        _sesClient = new AmazonSimpleEmailServiceClient(credentials, regionEndpoint);
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken) {
        var mimeMessage = BuildMimeMessage(message);

        var result = await SendViaAmazonAsync(mimeMessage, cancellationToken);

        return result;
    }

    private MimeMessage BuildMimeMessage(EmailMessage message) {
        var mimeMessage = _mimeMessageBuilder.BuildMessage(message.From,
                                                           message.To,
                                                           message.Cc,
                                                           message.Bcc,
                                                           message.Subject,
                                                           message.HtmlBody,
                                                           BodyFormats.Html,
                                                           message.Attachments);

        return mimeMessage;
    }

    private async Task<EmailSendResult> SendViaAmazonAsync(MimeMessage mimeMessage, CancellationToken cancellationToken) {
        try {
            using (var messageStream = mimeMessage.ToStream()) {
                var req = new SendRawEmailRequest();
                req.RawMessage = new RawMessage(messageStream);

                await _sesClient.SendRawEmailAsync(req, cancellationToken);
            }

            return new EmailSendResult([]);
        } catch (AccountSendingPausedException ex) {
            return new EmailSendResult([$"{ex.StatusCode}", $"{ex.Message}"]);
        } catch (MailFromDomainNotVerifiedException ex) {
            return new EmailSendResult([$"{ex.StatusCode}", $"{ex.Message}"]);
        } catch (MessageRejectedException ex) {
            return new EmailSendResult([$"{ex.StatusCode}", $"{ex.Message}"]);
        } catch (Exception ex) {
            return new EmailSendResult([ex.Message]);
        }
    }
}
