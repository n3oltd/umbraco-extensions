using MimeKit;
using System.IO;

namespace N3O.Umbraco.Email.Extensions;

public static class MimeMessageExtensions {
    public static MemoryStream ToStream(this MimeMessage message) {
        var stream = new MemoryStream();

        message.WriteTo(stream);

        return stream;
    }
}
