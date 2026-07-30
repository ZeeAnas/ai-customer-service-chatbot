using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Entities;

namespace Chatbot.Api.Tests.Integration;

public sealed class FakeLeadNotificationService
    : ILeadNotificationService
{
    public int CallCount { get; private set; }

    public Lead? LastLead { get; private set; }

    public bool ShouldFail { get; set; }

    public Task NotifyNewLeadAsync(
        Lead lead,
        CancellationToken cancellationToken = default
    )
    {
        CallCount++;
        LastLead = lead;

        if (ShouldFail)
        {
            return Task.FromException(
                new InvalidOperationException(
                    "Simulated notification failure."
                )
            );
        }

        return Task.CompletedTask;
    }

    public void Reset()
    {
        CallCount = 0;
        LastLead = null;
        ShouldFail = false;
    }
}