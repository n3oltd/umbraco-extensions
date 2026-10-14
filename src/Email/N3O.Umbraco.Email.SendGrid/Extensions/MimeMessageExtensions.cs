using MimeKit;
using N3O.Umbraco.Extensions;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmailAddress = SendGrid.Helpers.Mail.EmailAddress;
using SendGridAttachment = SendGrid.Helpers.Mail.Attachment;

namespace N3O.Umbraco.Email.SendGrid.Extensions;

public static class MimeMessageExtensions {
    public static SendGridMessage ToSendGridMessage(this MimeMessage message) {
        var sendGridMessage = new SendGridMessage();
        sendGridMessage.From = GetSendGridAddress(message.From.Mailboxes.Single());
        sendGridMessage.Subject = message.Subject;
        sendGridMessage.HtmlContent = message.HtmlBody;
        sendGridMessage.PlainTextContent = message.TextBody;

        if (message.To.Any()) {
            var tos = message.To.GetSendGridAddresses();
            sendGridMessage.AddTos(tos);
        }

        if (message.Cc.Any()) {
            var ccAddresses = message.Cc.GetSendGridAddresses();
            sendGridMessage.AddCcs(ccAddresses);
        }

        if (message.Bcc.Any()) {
            var bcc = message.Bcc.GetSendGridAddresses();
            sendGridMessage.AddBccs(bcc);
        }

        if (message.Attachments.Any()) {
            sendGridMessage.Attachments = new List<SendGridAttachment>();
            sendGridMessage.Attachments.AddRange(message.Attachments.Select(GetSendGridAttachment));
        }

        return sendGridMessage;
    }

    private static EmailAddress GetSendGridAddress(this MailboxAddress mailboxAddress) {
        if (mailboxAddress.Name.HasValue()) {
            var name = mailboxAddress.Name.Replace(",", "").Replace(";", "");

            return new EmailAddress(mailboxAddress.Address, name);
        } else {
            return new EmailAddress(mailboxAddress.Address);
        }
    }

    private static List<EmailAddress> GetSendGridAddresses(this InternetAddressList list) {
        return list.Mailboxes.Select(address => address.GetSendGridAddress()).ToList();
    }

    private static SendGridAttachment GetSendGridAttachment(MimeEntity attachment) {
        using (var stream = new MemoryStream()) {
            string fileName;

            if (attachment is MimePart mimePart) {
                mimePart.Content.DecodeTo(stream);
                fileName = mimePart.FileName;
            } else if (attachment is MessagePart messagePart) {
                messagePart.Message.WriteTo(stream);
                fileName = messagePart.ContentDisposition?.FileName;
            } else {
                throw new Exception($"Unsupported attachment entity {attachment.GetType().Name.Quote()}");
            }

            var sendGridAttachment = new SendGridAttachment();
            sendGridAttachment.Disposition = attachment.ContentDisposition?.Disposition;
            sendGridAttachment.Type = attachment.ContentType.MimeType;
            sendGridAttachment.Filename = fileName;
            sendGridAttachment.ContentId = attachment.ContentId;
            sendGridAttachment.Content = Convert.ToBase64String(stream.ToArray());

            return sendGridAttachment;
        }
    }
}
