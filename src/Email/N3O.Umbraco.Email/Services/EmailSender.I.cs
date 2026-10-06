using N3O.Umbraco.Email.Models;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email;

public interface IEmailSender {
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
