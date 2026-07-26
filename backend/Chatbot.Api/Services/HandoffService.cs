using Chatbot.Api.Interfaces;
using Chatbot.Api.Models;

namespace Chatbot.Api.Services;

public sealed class HandoffService : IHandoffService
{
    private readonly ILogger<HandoffService> _logger;

    public HandoffService(
        ILogger<HandoffService> logger
    )
    {
        _logger = logger;
    }

    public Task ProcessAsync(
        HandoffRequestDto request,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            """
            New human handoff request received.

            Name: {Name}
            Email: {Email}
            Phone: {Phone}
            Message: {Message}
            Received at: {ReceivedAt}
            """,
            request.Name.Trim(),
            request.Email.Trim(),
            request.Phone?.Trim(),
            request.Message.Trim(),
            DateTimeOffset.UtcNow
        );

        return Task.CompletedTask;
    }
}