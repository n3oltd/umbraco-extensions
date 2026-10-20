using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI;

public interface IBudgetExceededMatcher {
    bool IsMatch(AIProviderException ex);
}
