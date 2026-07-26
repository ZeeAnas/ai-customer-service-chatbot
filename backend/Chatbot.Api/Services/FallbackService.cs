using Chatbot.Api.Interfaces;

namespace Chatbot.Api.Services;

public sealed class FallbackService : IFallbackService
{
    private static readonly string[] UncertaintyPhrases =
    [
        "i'm not sure",
        "i am not sure",
        "i don't know",
        "i do not know",
        "i couldn't find",
        "i could not find",
        "i don't have information",
        "i do not have information",
        "i don't have that information",
        "i do not have that information",
        "i have no information",
        "there is no information",
        "not enough information",
        "unable to confirm",
        "cannot confirm",
        "can't confirm",
        "please contact the shop directly",
        "contact the business directly",
        "contact us directly"
    ];

    public bool ShouldFallback(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return true;
        }

        return UncertaintyPhrases.Any(
            phrase => response.Contains(
                phrase,
                StringComparison.OrdinalIgnoreCase
            )
        );
    }

    public string GetFallbackResponse()
    {
        return
            "I couldn't find a reliable answer to your question. " +
            "Please fill out the contact form with your inquiry, " +
            "and our team will get back to you as soon as possible. " +
            "You can also call us at 403 03 035 during opening hours.";
    }
}