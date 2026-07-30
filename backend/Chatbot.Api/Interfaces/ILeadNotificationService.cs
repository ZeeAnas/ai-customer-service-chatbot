using Chatbot.Api.Models.Entities;

namespace Chatbot.Api.Interfaces;

public interface ILeadNotificationService
{
    Task NotifyNewLeadAsync(
        Lead lead,
        CancellationToken cancellationToken = default
    );
}