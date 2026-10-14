using MimeKit;
using N3O.Umbraco.Email.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email;

public interface IMimeMessageBuilder {
    Task<MimeMessage> BuildMessageAsync(EmailIdentity from,
                                        IEnumerable<EmailIdentity> to,
                                        IEnumerable<EmailIdentity> cc,
                                        IEnumerable<EmailIdentity> bcc,
                                        string subject,
                                        string htmlBody,
                                        IEnumerable<EmailAttachmentReq> attachments,
                                        CancellationToken cancellationToken = default);
}
