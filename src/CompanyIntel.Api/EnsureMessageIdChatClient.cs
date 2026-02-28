using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CompanyIntel.Api;

/// <summary>
/// Workaround: OllamaSharp doesn't set MessageId on streaming ChatResponseUpdate,
/// but Microsoft.Agents.AI.Hosting.AGUI passes it through as-is to SSE events.
/// CopilotKit's Zod schema rejects null messageId → ZodError.
/// </summary>
internal sealed class EnsureMessageIdChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        string? messageId = null;

        await foreach (
            var update in base.GetStreamingResponseAsync(messages, options, cancellationToken)
        )
        {
            if (update.MessageId is null)
            {
                messageId ??= Guid.NewGuid().ToString("N");
                update.MessageId = messageId;
            }

            yield return update;
        }
    }
}
