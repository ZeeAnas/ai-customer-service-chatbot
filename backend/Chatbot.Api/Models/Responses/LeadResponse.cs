using Chatbot.Api.Models.Enums;

namespace Chatbot.Api.Models.Responses;

public class LeadResponse
{
    public int Id { get; set; }

    public Guid ConversationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Message { get; set; } = string.Empty;

    public LeadStatus Status { get; set; }

    public string? StaffNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? ContactedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }
}