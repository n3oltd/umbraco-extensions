using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;

namespace N3O.Umbraco.AI.Anthropic;

public class BudgetExceededChatMiddleware : IAIChatMiddleware {
    public IChatClient Apply(IChatClient client) {
        return new BudgetExceededChatClient(client);
    }
}
