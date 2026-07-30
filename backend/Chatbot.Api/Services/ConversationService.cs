using Chatbot.Api.Data;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Api.Services;

public class ConversationService : IConversationService
{
    private readonly ChatbotDbContext _dbContext;

    public ConversationService(ChatbotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Conversation> GetOrCreateConversationAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException(
                "Session ID cannot be empty.",
                nameof(sessionId));
        }

        var normalizedSessionId = sessionId.Trim();

        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(
                existingConversation =>
                    existingConversation.SessionId == normalizedSessionId,
                cancellationToken);

        if (conversation is not null)
        {
            return conversation;
        }

        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            SessionId = normalizedSessionId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Conversations.Add(conversation);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return conversation;
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(conversation).State = EntityState.Detached;

            var existingConversation =
                await _dbContext.Conversations.FirstOrDefaultAsync(
                    storedConversation =>
                        storedConversation.SessionId == normalizedSessionId,
                    cancellationToken);

            if (existingConversation is not null)
            {
                return existingConversation;
            }

            throw;
        }
    }

    public async Task AddMessageAsync(
        Guid conversationId,
        string role,
        string content,
        CancellationToken cancellationToken)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Conversation ID cannot be empty.",
                nameof(conversationId));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException(
                "Message role cannot be empty.",
                nameof(role));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Message content cannot be empty.",
                nameof(content));
        }

        var normalizedRole = role.Trim().ToLowerInvariant();

        if (normalizedRole is not "user" and not "assistant")
        {
            throw new ArgumentException(
                "Message role must be either 'user' or 'assistant'.",
                nameof(role));
        }

        var conversationExists = await _dbContext.Conversations
            .AnyAsync(
                conversation => conversation.Id == conversationId,
                cancellationToken);

        if (!conversationExists)
        {
            throw new InvalidOperationException(
                $"Conversation '{conversationId}' was not found.");
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = normalizedRole,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Message>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Conversation ID cannot be empty.",
                nameof(conversationId));
        }

        return await _dbContext.Messages
            .AsNoTracking()
            .Where(message =>
                message.ConversationId == conversationId)
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Message>> GetMessagesBySessionIdAsync(
    string sessionId,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(sessionId))
    {
        throw new ArgumentException(
            "Session ID cannot be empty.",
            nameof(sessionId));
    }

    var normalizedSessionId = sessionId.Trim();

    return await _dbContext.Messages
        .AsNoTracking()
        .Where(message =>
            message.Conversation.SessionId == normalizedSessionId)
        .OrderBy(message => message.CreatedAt)
        .ThenBy(message => message.Id)
        .ToListAsync(cancellationToken);
}
}