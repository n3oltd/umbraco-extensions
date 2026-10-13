using N3O.Umbraco.Extensions;
using System.Collections.Generic;

namespace N3O.Umbraco.Email.Models;

public class EmailSendResult : Value {
    public EmailSendResult(IEnumerable<string> errors) {
        Errors = errors;
    }

    public EmailSendResult() : this([]) { }

    public IEnumerable<string> Errors { get; }

    public bool Success => Errors.None();

    public static EmailSendResult ForFailure(IEnumerable<string> errors) {
        return new EmailSendResult(errors);
    }

    public static EmailSendResult ForSuccess() {
        return new EmailSendResult();
    }
}
