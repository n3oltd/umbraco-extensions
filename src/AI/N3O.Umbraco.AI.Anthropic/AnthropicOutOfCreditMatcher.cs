using Anthropic.Exceptions;
using Anthropic.Models;
using N3O.Umbraco.AI.Errors;
using System;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.Anthropic;

public class AnthropicOutOfCreditMatcher : IAIErrorMatcher {
    private const string CreditBalanceTooLow = "credit balance is too low";

    public AIErrorCondition Condition => AIErrorCondition.OutOfCredit;

    public bool IsMatch(AIProviderException ex) {
        if (ex.InnerException is AnthropicBadRequestException badRequestException &&
            badRequestException.ErrorType == ErrorType.InvalidRequestError) {
            var message = AIErrorResponseBody.GetErrorMessage(badRequestException.ResponseBody);

            return message?.Contains(CreditBalanceTooLow, StringComparison.InvariantCultureIgnoreCase) == true;
        } else {
            return false;
        }
    }
}
