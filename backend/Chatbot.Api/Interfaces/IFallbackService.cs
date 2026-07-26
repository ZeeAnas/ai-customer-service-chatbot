namespace Chatbot.Api.Interfaces;

public interface IFallbackService
{
    bool ShouldFallback(string response);

    string GetFallbackResponse();
}