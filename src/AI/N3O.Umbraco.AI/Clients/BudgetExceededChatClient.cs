using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI;

public class BudgetExceededChatClient : DelegatingChatClient {
    private const string UserMessage = "This site has used this month's AI allowance. It starts again on the 1st, " +
                                       "or IT can add more.";

    private readonly IEnumerable<IBudgetExceededMatcher> _matchers;

    public BudgetExceededChatClient(IChatClient innerClient, IEnumerable<IBudgetExceededMatcher> matchers)
        : base(innerClient) {
        _matchers = matchers;
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
                                                              ChatOptions options = null,
                                                              CancellationToken cancellationToken = default) {
        try {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        } catch (AIProviderException ex) when (_matchers.Any(x => x.IsMatch(ex))) {
            throw Rewrite(ex);
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        var updates = base.GetStreamingResponseAsync(messages, options, cancellationToken);

        await using (var enumerator = updates.GetAsyncEnumerator(cancellationToken)) {
            while (await MoveNextAsync(enumerator)) {
                yield return enumerator.Current;
            }
        }
    }

    private async Task<bool> MoveNextAsync(IAsyncEnumerator<ChatResponseUpdate> enumerator) {
        try {
            return await enumerator.MoveNextAsync();
        } catch (AIProviderException ex) when (_matchers.Any(x => x.IsMatch(ex))) {
            throw Rewrite(ex);
        }
    }

    private static AIProviderException Rewrite(AIProviderException ex) {
        var info = new AIProviderErrorInfo(ex.Info.Category, UserMessage, ex.ProviderCode, ex.Info.RawMessage);

        return new AIProviderException(info, ex);
    }
}
