using System.Collections.Generic;

namespace N3O.Umbraco.Email.Models;

public class EmailMessage : Value {
    public EmailMessage(EmailIdentity from,
                        IEnumerable<EmailIdentity> to,
                        IEnumerable<EmailIdentity> cc,
                        IEnumerable<EmailIdentity> bcc,
                        string subject,
                        string htmlBody,
                        IEnumerable<EmailAttachment> attachments,
                        string tag) {
        From = from;
        To = to;
        Cc = cc;
        Bcc = bcc;
        Subject = subject;
        HtmlBody = htmlBody;
        Attachments = attachments;
        Tag = tag;
    }

    public EmailIdentity From { get; }
    public IEnumerable<EmailIdentity> To { get; }
    public IEnumerable<EmailIdentity> Cc { get; }
    public IEnumerable<EmailIdentity> Bcc { get; }
    public string Subject { get; }
    public string HtmlBody { get; }
    public IEnumerable<EmailAttachment> Attachments { get; }
    public string Tag { get; }
}
