using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Core.Providers.Errors;

namespace N3O.Umbraco.AI.Anthropic;

public class BudgetExceededChatClient : DelegatingChatClient {
    private const string BudgetExceeded = "budget_exceeded";
    private const string UserMessage = "This site has used this month's AI allowance. It starts again on the 1st, " +
                                       "or IT can add more.";

    public BudgetExceededChatClient(IChatClient innerClient) : base(innerClient) { }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
                                                              ChatOptions options = null,
                                                              CancellationToken cancellationToken = default) {
        try {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        } catch (AIProviderException ex) when (ex.ProviderCode == BudgetExceeded) {
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

    private static async Task<bool> MoveNextAsync(IAsyncEnumerator<ChatResponseUpdate> enumerator) {
        try {
            return await enumerator.MoveNextAsync();
        } catch (AIProviderException ex) when (ex.ProviderCode == BudgetExceeded) {
            throw Rewrite(ex);
        }
    }

    private static AIProviderException Rewrite(AIProviderException ex) {
        var info = new AIProviderErrorInfo(ex.Info.Category, UserMessage, ex.ProviderCode, ex.Info.RawMessage);

        return new AIProviderException(info, ex);
    }
}
