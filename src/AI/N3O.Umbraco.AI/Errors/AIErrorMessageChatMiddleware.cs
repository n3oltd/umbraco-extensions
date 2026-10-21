using Microsoft.Extensions.AI;
using N3O.Umbraco.Localization;
using System.Collections.Generic;
using Umbraco.AI.Core.Chat;

namespace N3O.Umbraco.AI.Errors;

public class AIErrorMessageChatMiddleware : IAIChatMiddleware {
    private readonly IEnumerable<IAIErrorMatcher> _matchers;
    private readonly ITextFormatter _textFormatter;

    public AIErrorMessageChatMiddleware(IEnumerable<IAIErrorMatcher> matchers, ITextFormatter textFormatter) {
        _matchers = matchers;
        _textFormatter = textFormatter;
    }

    public IChatClient Apply(IChatClient client) {
        return new AIErrorMessageChatClient(client, _matchers, _textFormatter);
    }
}
