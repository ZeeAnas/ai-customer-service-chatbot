using Chatbot.Api.Models.Requests;

namespace Chatbot.Api.Interfaces;

public interface IChatService
{
    Task<string> GetReplyAsync(
        List<ChatMessageRequest> messages,
        CancellationToken cancellationToken
    );
}