namespace Chatbot.Api.Interfaces;

public interface IBusinessHoursService
{
    bool IsOpenNow();

    string GetStatusMessage();
    string GetWeeklyScheduleMessage();
}