using Anthropic.Exceptions;
using Anthropic.Models;
using N3O.Umbraco.AI.Errors;
using System;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.Anthropic;

public class AnthropicOutOfCreditMatcher : IAIErrorMatcher {
    private const string CreditBalanceTooLow = "credit balance is too low";
    private const string EnforcedSpendLimitReached = "enforced_spend_limit_reached";
    private const string SpecifiedUsageLimitsReached = "You have reached your specified";

    public AIErrorCondition Condition => AIErrorCondition.OutOfCredit;

    public bool IsMatch(AIProviderException ex) {
        if (ex.InnerException is AnthropicBadRequestException badRequestException &&
            badRequestException.ErrorType == ErrorType.InvalidRequestError) {
            var message = AIErrorResponseBody.GetErrorMessage(badRequestException.ResponseBody);

            return message?.Contains(CreditBalanceTooLow, StringComparison.InvariantCultureIgnoreCase) == true ||
                   message?.StartsWith(SpecifiedUsageLimitsReached, StringComparison.InvariantCultureIgnoreCase) == true;
        } else if (ex.InnerException is AnthropicRateLimitException rateLimitException) {
            var errorCode = AIErrorResponseBody.GetErrorDetailsCode(rateLimitException.ResponseBody);

            return errorCode == EnforcedSpendLimitReached;
        } else {
            return false;
        }
    }
}
