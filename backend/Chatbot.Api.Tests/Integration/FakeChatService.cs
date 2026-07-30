using System.Runtime.CompilerServices;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Requests;

namespace Chatbot.Api.Tests.Integration;

public sealed class FakeChatService : IChatService
{
    private const string FakeResponse =
        "This is a fake integration-test response.";

    private readonly IConversationService _conversationService;

    public FakeChatService(
        IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        string sessionId,
        List<ChatMessageRequest> messages,
        [EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException(
                "Session ID cannot be empty.",
                nameof(sessionId)
            );
        }

        var conversation =
            await _conversationService.GetOrCreateConversationAsync(
                sessionId,
                cancellationToken
            );

        var latestUserMessage = messages.LastOrDefault(
            message => message.Role.Equals(
                "user",
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (latestUserMessage is not null &&
            !string.IsNullOrWhiteSpace(
                latestUserMessage.Content
            ))
        {
            await _conversationService.AddMessageAsync(
                conversation.Id,
                "user",
                latestUserMessage.Content,
                cancellationToken
            );
        }

        await _conversationService.AddMessageAsync(
            conversation.Id,
            "assistant",
            FakeResponse,
            cancellationToken
        );

        yield return FakeResponse;
    }
}