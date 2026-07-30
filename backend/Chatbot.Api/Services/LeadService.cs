using Chatbot.Api.Data;
using Chatbot.Api.Exceptions;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Entities;
using Chatbot.Api.Models.Enums;
using Chatbot.Api.Models.Requests;
using Chatbot.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Api.Services;

public class LeadService : ILeadService
{
    private readonly ChatbotDbContext _dbContext;
    private readonly ILogger<LeadService> _logger;
    private readonly ILeadNotificationService _leadNotificationService;

    public LeadService(
        ChatbotDbContext dbContext,
        ILeadNotificationService leadNotificationService,
        ILogger<LeadService> logger
    )
    {
        _dbContext = dbContext;
        _leadNotificationService = leadNotificationService;
        _logger = logger;
    }

    public async Task<LeadResponse> CreateAsync(
        CreateLeadRequest request,
        CancellationToken cancellationToken
    )
    {
        var normalizedSessionId = request.SessionId.Trim();

        var conversation =
            await _dbContext.Conversations.SingleOrDefaultAsync(
                conversation =>
                    conversation.SessionId == normalizedSessionId,
                cancellationToken
            );

        if (conversation is null)
        {
            _logger.LogWarning(
                "Lead creation failed because session {SessionId} does not have a conversation.",
                normalizedSessionId
            );

            throw new ResourceNotFoundException(
                $"Conversation for session '{normalizedSessionId}' was not found."
            );
        }

        var normalizedEmail =
            NormalizeOptionalValue(request.Email);

        var normalizedPhone =
            NormalizeOptionalValue(request.Phone);

        var duplicateLeadExists =
            await _dbContext.Leads.AnyAsync(
                lead =>
                    lead.ConversationId == conversation.Id &&
                    (
                        (
                            normalizedEmail != null &&
                            lead.Email == normalizedEmail
                        )
                        ||
                        (
                            normalizedPhone != null &&
                            lead.Phone == normalizedPhone
                        )
                    ),
                cancellationToken
            );

        if (duplicateLeadExists)
        {
            _logger.LogWarning(
                "Duplicate lead submission prevented for conversation {ConversationId}.",
                conversation.Id
            );

            throw new ConflictException(
                "A lead with these contact details has already been submitted for this conversation."
            );
        }

        var nowUtc = DateTime.UtcNow;

        var lead = new Lead
        {
            ConversationId = conversation.Id,
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            Phone = normalizedPhone,
            Message = request.Message.Trim(),
            Status = LeadStatus.New,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        _dbContext.Leads.Add(lead);

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        _logger.LogInformation(
            "Lead {LeadId} created for conversation {ConversationId} and session {SessionId}.",
            lead.Id,
            lead.ConversationId,
            normalizedSessionId
        );
          try
{
    await _leadNotificationService.NotifyNewLeadAsync(
        lead,
        cancellationToken
    );
}
catch (OperationCanceledException exception)
    when (cancellationToken.IsCancellationRequested)
{
    _logger.LogWarning(
        exception,
        "Staff notification was cancelled after lead {LeadId} was saved.",
        lead.Id
    );
}
catch (Exception exception)
{
    _logger.LogError(
        exception,
        "Staff notification failed after lead {LeadId} was saved.",
        lead.Id
    );
}

        return MapToResponse(lead);
    }

    public async Task<IReadOnlyList<LeadResponse>> GetAllAsync(
        LeadStatus? status,
        CancellationToken cancellationToken
    )
    {
        var query = _dbContext.Leads
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(
                lead => lead.Status == status.Value
            );
        }

        var leads = await query
            .OrderByDescending(lead => lead.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return leads
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<LeadResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var lead = await _dbContext.Leads
            .AsNoTracking()
            .SingleOrDefaultAsync(
                lead => lead.Id == id,
                cancellationToken
            );

        if (lead is null)
        {
            throw new ResourceNotFoundException(
                $"Lead with ID '{id}' was not found."
            );
        }

        return MapToResponse(lead);
    }

    public async Task<LeadResponse> UpdateAsync(
        int id,
        UpdateLeadRequest request,
        CancellationToken cancellationToken
    )
    {
        var lead = await _dbContext.Leads
            .SingleOrDefaultAsync(
                lead => lead.Id == id,
                cancellationToken
            );

        if (lead is null)
        {
            throw new ResourceNotFoundException(
                $"Lead with ID '{id}' was not found."
            );
        }

        if (!request.Status.HasValue)
        {
            throw new ConflictException(
                "A lead status is required."
            );
        }

        var newStatus = request.Status.Value;

        ValidateStatusTransition(
            lead.Status,
            newStatus
        );

        var nowUtc = DateTime.UtcNow;

        if (lead.Status != newStatus)
        {
            if (newStatus == LeadStatus.Contacted)
            {
                lead.ContactedAtUtc ??= nowUtc;
            }

            if (newStatus == LeadStatus.Closed)
            {
                lead.ClosedAtUtc = nowUtc;
            }

            lead.Status = newStatus;
        }

        lead.StaffNotes =
            NormalizeOptionalValue(request.StaffNotes);

        lead.UpdatedAtUtc = nowUtc;

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        _logger.LogInformation(
            "Lead {LeadId} updated to status {LeadStatus}.",
            lead.Id,
            lead.Status
        );

        return MapToResponse(lead);
    }

    private static void ValidateStatusTransition(
        LeadStatus currentStatus,
        LeadStatus newStatus
    )
    {
        if (currentStatus == newStatus)
        {
            return;
        }

        var isAllowed =
            currentStatus switch
            {
                LeadStatus.New =>
                    newStatus is
                        LeadStatus.Contacted or
                        LeadStatus.Closed,

                LeadStatus.Contacted =>
                    newStatus is
                        LeadStatus.Qualified or
                        LeadStatus.Closed,

                LeadStatus.Qualified =>
                    newStatus == LeadStatus.Closed,

                LeadStatus.Closed => false,

                _ => false
            };

        if (!isAllowed)
        {
            throw new ConflictException(
                $"Lead status cannot change from '{currentStatus}' to '{newStatus}'."
            );
        }
    }

    private static string? NormalizeOptionalValue(
        string? value
    )
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static LeadResponse MapToResponse(
        Lead lead
    )
    {
        return new LeadResponse
        {
            Id = lead.Id,
            ConversationId = lead.ConversationId,
            Name = lead.Name,
            Email = lead.Email,
            Phone = lead.Phone,
            Message = lead.Message,
            Status = lead.Status,
            StaffNotes = lead.StaffNotes,
            CreatedAtUtc = lead.CreatedAtUtc,
            UpdatedAtUtc = lead.UpdatedAtUtc,
            ContactedAtUtc = lead.ContactedAtUtc,
            ClosedAtUtc = lead.ClosedAtUtc
        };
    }
}