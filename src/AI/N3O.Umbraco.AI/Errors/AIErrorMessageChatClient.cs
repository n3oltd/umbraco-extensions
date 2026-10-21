using Microsoft.Extensions.AI;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.Errors;

public class AIErrorMessageChatClient : DelegatingChatClient {
    private readonly IEnumerable<IAIErrorMatcher> _matchers;
    private readonly ITextFormatter _textFormatter;

    public AIErrorMessageChatClient(IChatClient innerClient,
                                    IEnumerable<IAIErrorMatcher> matchers,
                                    ITextFormatter textFormatter)
        : base(innerClient) {
        _matchers = matchers;
        _textFormatter = textFormatter;
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
                                                              ChatOptions options = null,
                                                              CancellationToken cancellationToken = default) {
        try {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        } catch (AIProviderException ex) when (IsMatch(ex)) {
            throw Rewrite(ex);
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
                                                                                         ChatOptions options = null,
                                                                                         [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        var updates = base.GetStreamingResponseAsync(messages, options, cancellationToken);

        await using (var enumerator = updates.GetAsyncEnumerator(cancellationToken)) {
            while (await MoveNextAsync(enumerator)) {
                yield return enumerator.Current;
            }
        }
    }

    private string GetUserMessage(AIErrorCondition condition) {
        if (condition == AIErrorCondition.AllowanceUsedUp) {
            return _textFormatter.Format<Strings>(s => s.AllowanceUsedUp);
        } else if (condition == AIErrorCondition.OutOfCredit) {
            return _textFormatter.Format<Strings>(s => s.OutOfCredit);
        } else {
            throw UnrecognisedValueException.For(condition);
        }
    }

    private bool IsMatch(AIProviderException ex) {
        return _matchers.Any(x => x.IsMatch(ex));
    }

    private async Task<bool> MoveNextAsync(IAsyncEnumerator<ChatResponseUpdate> enumerator) {
        try {
            return await enumerator.MoveNextAsync();
        } catch (AIProviderException ex) when (IsMatch(ex)) {
            throw Rewrite(ex);
        }
    }

    private AIProviderException Rewrite(AIProviderException ex) {
        var matcher = _matchers.First(x => x.IsMatch(ex));
        var userMessage = GetUserMessage(matcher.Condition);
        var info = new AIProviderErrorInfo(ex.Info.Category, userMessage, ex.ProviderCode, ex.Info.RawMessage);

        return new AIProviderException(info, ex);
    }

    public class Strings : CodeStrings {
        public string AllowanceUsedUp => "This site has used this month's AI allowance. It starts again on the 1st, or IT can add more.";
        public string OutOfCredit => "The AI account for this site has run out of credit. Contact IT to top it up.";
    }
}
