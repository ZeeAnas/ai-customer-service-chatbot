namespace Chatbot.Api.Configuration;

public sealed class BusinessHoursOptions
{
    public const string SectionName = "BusinessHours";

    public string TimeZoneId { get; set; } = "Europe/Oslo";

    public Dictionary<DayOfWeek, DailyBusinessHours> Schedule { get; set; }
        = new();
}

public sealed class DailyBusinessHours
{
    public bool IsClosed { get; set; }

    public TimeOnly? OpensAt { get; set; }

    public TimeOnly? ClosesAt { get; set; }
}