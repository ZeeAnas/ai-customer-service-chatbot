using Chatbot.Api.Models.Requests;

namespace Chatbot.Api.Interfaces;

public interface IChatService
{
    IAsyncEnumerable<string> StreamReplyAsync(
    string sessionId,
    List<ChatMessageRequest> messages,
    CancellationToken cancellationToken);
}