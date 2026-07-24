using Chatbot.Api.Models.Requests;

namespace Chatbot.Api.Interfaces;

public interface IChatService
{
    IAsyncEnumerable<string> StreamReplyAsync(
        List<ChatMessageRequest> messages,
        CancellationToken cancellationToken
    );
}