using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.Anthropic;

public class AnthropicBudgetExceededMatcher : IBudgetExceededMatcher {
    private const string BudgetExceeded = "budget_exceeded";

    public bool IsMatch(AIProviderException ex) {
        return ex.ProviderCode == BudgetExceeded;
    }
}
