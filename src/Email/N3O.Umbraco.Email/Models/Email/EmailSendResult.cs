using N3O.Umbraco.Extensions;
using System.Collections.Generic;

namespace N3O.Umbraco.Email.Models;

public class EmailSendResult : Value {
    public EmailSendResult(IEnumerable<string> errors) {
        Errors = errors;
    }

    public IEnumerable<string> Errors { get; }

    public bool Success => Errors.None();
}
