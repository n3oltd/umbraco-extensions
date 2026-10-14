using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using MimeKit;
using N3O.Umbraco.Email.Extensions;
using N3O.Umbraco.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email.Amazon;

public class AmazonSender : IEmailSender {
    private readonly AmazonSimpleEmailServiceClient _sesClient;

    public AmazonSender(string accessKey, string secretKey, string regionCode) {
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var regionEndpoint = typeof(RegionEndpoint).GetConstantOrStaticValues<RegionEndpoint>()
                                                   .SingleOrDefault(x => x.SystemName.EqualsInvariant(regionCode));

        _sesClient = new AmazonSimpleEmailServiceClient(credentials, regionEndpoint);
    }

    public async Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default) {
        using (var messageStream = message.ToStream()) {
            var req = new SendRawEmailRequest();
            req.RawMessage = new RawMessage(messageStream);

            var response = await _sesClient.SendRawEmailAsync(req, cancellationToken);

            return response.MessageId;
        }
    }
}
