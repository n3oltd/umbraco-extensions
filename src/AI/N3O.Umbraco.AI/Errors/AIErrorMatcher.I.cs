using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.Errors;

public interface IAIErrorMatcher {
    AIErrorCondition Condition { get; }
    bool IsMatch(AIProviderException ex);
}
