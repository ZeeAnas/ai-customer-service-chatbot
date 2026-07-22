namespace Chatbot.Api.Exceptions;

public class OpenAiServiceException : Exception
{
    public int StatusCode { get; }

    public OpenAiServiceException(
        string message,
        int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }
}