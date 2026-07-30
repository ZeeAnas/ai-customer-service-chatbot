using Chatbot.Api.Models.Enums;

namespace Chatbot.Api.Models.Entities;

public class Lead
{
    public int Id { get; set; }

    public Guid ConversationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Message { get; set; } = string.Empty;

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public string? StaffNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; } =
        DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } =
        DateTime.UtcNow;

    public DateTime? ContactedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    public Conversation Conversation { get; set; } =
        null!;
}