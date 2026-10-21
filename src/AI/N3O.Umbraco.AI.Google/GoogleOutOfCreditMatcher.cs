using Google.GenAI;
using N3O.Umbraco.AI.Errors;
using System.Net;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.Google;

public class GoogleOutOfCreditMatcher : IAIErrorMatcher {
    public AIErrorCondition Condition => AIErrorCondition.OutOfCredit;

    public bool IsMatch(AIProviderException ex) {
        if (ex.InnerException is ClientError clientError) {
            return clientError.StatusCode == (int) HttpStatusCode.PaymentRequired;
        } else {
            return false;
        }
    }
}
