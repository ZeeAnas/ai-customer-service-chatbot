using Chatbot.Api.Models;

namespace Chatbot.Api.Interfaces;

public interface IHandoffService
{
    Task ProcessAsync(
        HandoffRequestDto request,
        CancellationToken cancellationToken
    );
}