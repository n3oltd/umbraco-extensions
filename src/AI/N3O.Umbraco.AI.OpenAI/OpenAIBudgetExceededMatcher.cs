using N3O.Umbraco.AI.Extensions;
using System.ClientModel;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.OpenAI;

public class OpenAIBudgetExceededMatcher : IBudgetExceededMatcher {
    private const string BudgetExceeded = "budget_exceeded";

    public bool IsMatch(AIProviderException ex) {
        var clientResultException = ex.InnerException as ClientResultException;

        return clientResultException?.GetErrorType() == BudgetExceeded;
    }
}
