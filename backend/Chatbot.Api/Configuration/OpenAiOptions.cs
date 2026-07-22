namespace Chatbot.Api.Configuration;

public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
}