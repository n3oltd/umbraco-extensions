using N3O.Umbraco.AI.Errors;
using N3O.Umbraco.AI.Extensions;
using System.ClientModel;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.LiteLLM;

public class LiteLLMAllowanceUsedUpMatcher : IAIErrorMatcher {
    private const string BudgetExceeded = "budget_exceeded";

    public AIErrorCondition Condition => AIErrorCondition.AllowanceUsedUp;

    public bool IsMatch(AIProviderException ex) {
        if (ex.InnerException is ClientResultException clientResultException) {
            return ex.ProviderCode == BudgetExceeded || clientResultException.GetErrorType() == BudgetExceeded;
        } else {
            return ex.ProviderCode == BudgetExceeded;
        }
    }
}
