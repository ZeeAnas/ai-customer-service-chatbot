using Chatbot.Api.Interfaces;

namespace Chatbot.Api.Services;

public class PromptService : IPromptService
{
    private readonly string _systemPrompt;

    public PromptService(IWebHostEnvironment environment)
    {
        var promptPath = Path.Combine(
            environment.ContentRootPath,
            "Prompts",
            "MontanaBarberSystemPrompt.txt"
        );

        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException(
                "The Montana Barber system prompt file was not found.",
                promptPath
            );
        }

        _systemPrompt = File.ReadAllText(promptPath);

        if (string.IsNullOrWhiteSpace(_systemPrompt))
        {
            throw new InvalidOperationException(
                "The Montana Barber system prompt file is empty."
            );
        }
    }

    public string GetSystemPrompt()
    {
        return _systemPrompt;
    }
}