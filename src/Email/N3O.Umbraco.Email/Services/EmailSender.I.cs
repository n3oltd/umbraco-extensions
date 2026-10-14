using MimeKit;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email;

public interface IEmailSender {
    Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default);
}
