namespace Chatbot.Api.Interfaces;

public interface IChatService
{
    Task<string> GetReplyAsync(
        string message,
        // CancellationToken lets ASP.NET stop the OpenAI request when the browser disconnects or cancels the request.
        CancellationToken cancellationToken
    );
}