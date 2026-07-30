using Chatbot.Api.Models.Entities;

namespace Chatbot.Api.Interfaces;

public interface IConversationService
{
    Task<Conversation> GetOrCreateConversationAsync(
        string sessionId,
        CancellationToken cancellationToken);

    Task AddMessageAsync(
        Guid conversationId,
        string role,
        string content,
        CancellationToken cancellationToken);

    Task<List<Message>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<List<Message>> GetMessagesBySessionIdAsync(

        string sessionId,

        CancellationToken cancellationToken);
}