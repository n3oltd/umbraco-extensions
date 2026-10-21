using N3O.Umbraco.AI.Errors;
using N3O.Umbraco.AI.Extensions;
using System.ClientModel;
using System.Net;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.OpenAI;

public class OpenAIOutOfCreditMatcher : IAIErrorMatcher {
    private const string CreditBalanceExhausted = "credit_balance_exhausted";
    private const string InsufficientQuota = "insufficient_quota";

    public AIErrorCondition Condition => AIErrorCondition.OutOfCredit;

    public bool IsMatch(AIProviderException ex) {
        if (ex.InnerException is ClientResultException clientResultException &&
            clientResultException.Status == (int) HttpStatusCode.TooManyRequests) {
            return clientResultException.GetErrorType() == InsufficientQuota ||
                   clientResultException.GetErrorCode() == CreditBalanceExhausted;
        } else {
            return false;
        }
    }
}
