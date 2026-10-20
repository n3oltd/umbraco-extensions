using Microsoft.Extensions.AI;
using System.Collections.Generic;
using Umbraco.AI.Core.Chat;

namespace N3O.Umbraco.AI;

public class BudgetExceededChatMiddleware : IAIChatMiddleware {
    private readonly IEnumerable<IBudgetExceededMatcher> _matchers;

    public BudgetExceededChatMiddleware(IEnumerable<IBudgetExceededMatcher> matchers) {
        _matchers = matchers;
    }

    public IChatClient Apply(IChatClient client) {
        return new BudgetExceededChatClient(client, _matchers);
    }
}
