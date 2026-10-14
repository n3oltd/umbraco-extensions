using MimeKit;
using N3O.Umbraco.Email.Extensions;
using N3O.Umbraco.Email.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Storage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email;

public class MimeMessageBuilder : IMimeMessageBuilder {
    private readonly IVolume _volume;

    public MimeMessageBuilder(IVolume volume) {
        _volume = volume;
    }

    public async Task<MimeMessage> BuildMessageAsync(EmailIdentity from,
                                                     IEnumerable<EmailIdentity> to,
                                                     IEnumerable<EmailIdentity> cc,
                                                     IEnumerable<EmailIdentity> bcc,
                                                     string subject,
                                                     string htmlBody,
                                                     IEnumerable<EmailAttachmentReq> attachments,
                                                     CancellationToken cancellationToken = default) {
        var message = new MimeMessage();

        message.From.Add(from.ToMailboxAddress());

        to.OrEmpty().Do(x => message.To.Add(x.ToMailboxAddress()));

        cc.OrEmpty().Do(x => message.Cc.Add(x.ToMailboxAddress()));

        bcc.OrEmpty().Do(x => message.Bcc.Add(x.ToMailboxAddress()));

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = htmlBody;

        foreach (var attachment in attachments.OrEmpty()) {
            var storageFolder = await _volume.GetStorageFolderAsync(attachment.StorageFolderName);
            var blob = await storageFolder.GetFileAsync(attachment.Filename, cancellationToken);

            using (blob.Stream) {
                var contentType = ContentType.Parse(attachment.ContentType);

                await bodyBuilder.Attachments.AddAsync(attachment.Name, blob.Stream, contentType, cancellationToken);
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }
}
