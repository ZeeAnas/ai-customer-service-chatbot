using Chatbot.Api.Configuration;
using Chatbot.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace Chatbot.Api.Services;

public sealed class BusinessHoursService : IBusinessHoursService
{
    private readonly BusinessHoursOptions _options;

    public BusinessHoursService(
        IOptions<BusinessHoursOptions> options
    )
    {
        _options = options.Value;
    }

    public bool IsOpenNow()
    {
        var localNow = GetLocalNow();

        if (!_options.Schedule.TryGetValue(
                localNow.DayOfWeek,
                out var todayHours))
        {
            return false;
        }

        if (todayHours.IsClosed ||
            todayHours.OpensAt is null ||
            todayHours.ClosesAt is null)
        {
            return false;
        }

        var currentTime = TimeOnly.FromDateTime(localNow);

        return currentTime >= todayHours.OpensAt.Value &&
               currentTime < todayHours.ClosesAt.Value;
    }

    public string GetStatusMessage()
    {
        var localNow = GetLocalNow();

        if (!_options.Schedule.TryGetValue(
                localNow.DayOfWeek,
                out var todayHours))
        {
            return "Business hours are currently unavailable.";
        }

        if (todayHours.IsClosed ||
            todayHours.OpensAt is null ||
            todayHours.ClosesAt is null)
        {
            return "We are closed today.";
        }

        var currentTime = TimeOnly.FromDateTime(localNow);

        if (currentTime < todayHours.OpensAt.Value)
        {
            return $"We are currently closed and open today at {todayHours.OpensAt.Value:HH\\:mm}.";
        }

        if (currentTime < todayHours.ClosesAt.Value)
        {
            return $"We are currently open until {todayHours.ClosesAt.Value:HH\\:mm}.";
        }

        return "We are currently closed.";
    }
    public string GetWeeklyScheduleMessage()
{
    return """
           Monday: 10:00–20:00
           Tuesday: 10:00–20:00
           Wednesday: 10:00–20:00
           Thursday: 10:00–20:00
           Friday: 10:00–20:00
           Saturday: 10:00–18:00
           Sunday: Closed
           """;
}

    private DateTime GetLocalNow()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            _options.TimeZoneId
        );

        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            timeZone
        );
    }
}