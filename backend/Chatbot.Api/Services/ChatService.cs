using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chatbot.Api.Configuration;
using Chatbot.Api.Interfaces;
using Microsoft.Extensions.Options;
using Chatbot.Api.Exceptions;

namespace Chatbot.Api.Services;

public class ChatService : IChatService
{
   private const string SystemPrompt = """
    You are the customer-service assistant for Montana Barber Shop in Oslo.

    Your job is to answer customer questions clearly, politely and briefly
    using only the verified business information provided below.

    Business:
    Montana Barber Shop is a modern and stylish barbershop rooted in
    traditional barbering. The shop offers services such as precise
    haircuts, skin fades, beard grooming and hot-towel shaving experiences.

    Address:
    Konghellegata 14, 0572 Oslo, Norway.

    Contact information:
    - Phone: 403 03 035
    - Email: montanabarbershop900@gmail.com
    - Website: https://montanabarber.no/

    Services, prices and estimated durations:
    - Men's haircut: 500 NOK, approximately 30 minutes.
    - Skin fade: 500 NOK, approximately 30 minutes.
    - Men's haircut and beard: 700 NOK, approximately 1 hour.
    - Skin fade and beard: 700 NOK, approximately 45 minutes.
    - Beard service: 350 NOK, approximately 30 minutes.
    - Children's haircut: 400 NOK, approximately 30 minutes.
    - Student haircut: 450 NOK, approximately 30 minutes.
    - Women's haircut: 600 NOK, approximately 30 minutes.
    - Montana Full Package: 1000 NOK, approximately 1 hour.
    - Beard colouring: 500 NOK, approximately 30 minutes.
    - Hair wash and styling: 200 NOK, approximately 15 minutes.

    Booking:
    - Customers can book an appointment online through the Montana Barber
      Shop website.
    - When a customer wants to make a booking, guide them to the online
      booking option on the website.
    - Do not claim that you have completed, changed or cancelled a booking,
      because you do not yet have access to the booking system.

    Response rules:
    - Answer in the same language as the customer whenever possible.
    - Keep answers friendly, natural and concise.
    - Use NOK when mentioning prices.
    - Do not invent opening hours, payment methods, cancellation policies,
      available appointment times, employee names or other information that
      is not included above.
    - If the requested information is unavailable, say that you do not have
      that information and recommend contacting Montana Barber Shop directly.
    - Never pretend to have checked live booking availability.
    """;

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<ChatService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetReplyAsync(
    string message,
    CancellationToken cancellationToken)
{
    ValidateConfiguration();

    _logger.LogInformation(
        "Sending request to OpenAI using model {Model}",
        _options.Model
    );

    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        $"{_options.BaseUrl}responses"
    );

    request.Headers.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            _options.ApiKey
        );

    var requestBody = new
    {
        model = _options.Model,
        instructions = SystemPrompt,
        input = message
    };

    request.Content = JsonContent.Create(requestBody);

    using var response = await _httpClient.SendAsync(
        request,
        cancellationToken
    );

    var responseBody =
        await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogError(
            "OpenAI request failed with status code {StatusCode}",
            (int)response.StatusCode
        );

        throw new OpenAiServiceException(
            "The OpenAI service returned an unsuccessful response.",
            (int)response.StatusCode
        );
    }

    var reply = ExtractReply(responseBody);

    _logger.LogInformation(
        "OpenAI request completed successfully"
    );

    return reply;
}

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "The OpenAI API key is not configured."
            );
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException(
                "The OpenAI base URL is not configured."
            );
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException(
                "The OpenAI model is not configured."
            );
        }
    }

    private static string ExtractReply(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);

        var output = document.RootElement.GetProperty("output");

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content))
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                var type = contentItem.GetProperty("type").GetString();

                if (type == "output_text")
                {
                    return contentItem
                        .GetProperty("text")
                        .GetString()
                        ?? "The assistant returned an empty response.";
                }
            }
        }

        throw new InvalidOperationException(
            "No text answer was found in the OpenAI response."
        );
    }
}