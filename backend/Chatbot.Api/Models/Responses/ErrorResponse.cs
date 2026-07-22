namespace Chatbot.Api.Models.Responses;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;

// TraceId helps identify the matching request in backend logs.
    public string? TraceId { get; set; }
}