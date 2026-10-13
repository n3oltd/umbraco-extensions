using N3O.Umbraco.Extensions;
using System.Collections.Generic;

namespace N3O.Umbraco.Email.Models;

public class EmailSendResult : Value {
    public EmailSendResult(string messageId, IEnumerable<string> errors) {
        MessageId = messageId;
        Errors = errors;
    }

    public EmailSendResult(string messageId) : this(messageId, []) { }

    public EmailSendResult(IEnumerable<string> errors) : this(null, errors) { }

    public string MessageId { get; }
    public IEnumerable<string> Errors { get; }

    public bool Success => Errors.None();

    public static EmailSendResult ForFailure(IEnumerable<string> errors) {
        return new EmailSendResult(errors);
    }

    public static EmailSendResult ForSuccess(string messageId) {
        return new EmailSendResult(messageId);
    }
}
