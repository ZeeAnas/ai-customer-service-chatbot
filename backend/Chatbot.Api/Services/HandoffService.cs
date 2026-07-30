using Chatbot.Api.Data;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Models;
using Chatbot.Api.Models.Entities;
using Chatbot.Api.Models.Enums;

namespace Chatbot.Api.Services;

public sealed class HandoffService : IHandoffService
{
    private readonly ChatbotDbContext _dbContext;
    private readonly ILogger<HandoffService> _logger;

    public HandoffService(
        ChatbotDbContext dbContext,
        ILogger<HandoffService> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ProcessAsync(
        HandoffRequestDto request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var lead = new Lead
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim(),
            Message = request.Message.Trim(),
            Status = LeadStatus.New,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Leads.Add(lead);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Human handoff lead {LeadId} was saved successfully at {CreatedAtUtc}.",
            lead.Id,
            lead.CreatedAtUtc
        );
    }
}