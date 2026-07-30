namespace Chatbot.Api.Models.Entities;

public class Conversation
{
    public Guid Id { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } =
        new List<Message>();

    public ICollection<Lead> Leads { get; set; } =
        new List<Lead>();
}