namespace Chatbot.Api.Configuration;

public class LeadNotificationOptions
{
    public const string SectionName = "LeadNotification";

    public bool Enabled { get; set; }

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;
}