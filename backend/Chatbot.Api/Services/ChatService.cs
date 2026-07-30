using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Chatbot.Api.Configuration;
using Chatbot.Api.Exceptions;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Requests;
using Microsoft.Extensions.Options;

namespace Chatbot.Api.Services;

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<ChatService> _logger;
    private readonly IPromptService _promptService;
    private readonly IBusinessHoursService _businessHoursService;
    private readonly IFallbackService _fallbackService;
    private readonly IConversationService _conversationService;

    public ChatService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<ChatService> logger,
        IPromptService promptService,
        IBusinessHoursService businessHoursService,
        IFallbackService fallbackService,
        IConversationService conversationService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _promptService = promptService;
        _businessHoursService = businessHoursService;
        _fallbackService = fallbackService;
        _conversationService = conversationService;
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        string sessionId,
        List<ChatMessageRequest> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException(
                "Session ID cannot be empty.",
                nameof(sessionId));
        }

        var conversation =
            await _conversationService.GetOrCreateConversationAsync(
                sessionId,
                cancellationToken);

        var latestUserMessage = messages
            .LastOrDefault(message =>
                string.Equals(
                    message.Role,
                    "user",
                    StringComparison.OrdinalIgnoreCase));

        if (latestUserMessage is not null &&
            !string.IsNullOrWhiteSpace(latestUserMessage.Content))
        {
            await _conversationService.AddMessageAsync(
                conversation.Id,
                "user",
                latestUserMessage.Content,
                cancellationToken);
        }
        ValidateConfiguration();

        _logger.LogInformation(
            "Sending streaming request to OpenAI using model {Model}",
            _options.Model);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}responses");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey);

        var openAiMessages = messages.Select(message => new
        {
            role = message.Role,
            content = new[]
            {
                new
                {
                    type = message.Role == "assistant"
                        ? "output_text"
                        : "input_text",
                    text = message.Content
                }
            }
        }).ToList();

        var businessHoursStatus =
            _businessHoursService.GetStatusMessage();

        var weeklyBusinessHours =
            _businessHoursService.GetWeeklyScheduleMessage();

        var systemInstructions =
            $"""
            {_promptService.GetSystemPrompt()}

            Current business-hours status:
            {businessHoursStatus}

            Weekly opening hours:
            {weeklyBusinessHours}

            Business-hours rules:

            1. Use the current business-hours status when answering whether
               the business is open or closed right now.

            2. Use the weekly opening hours when answering questions about
               opening times, closing times, specific days, or future visits.

            3. When the user asks for the opening hours generally, provide
               the weekly schedule.

            4. Never guess, invent, or contradict the business-hours
               information provided above.

            5. Do not say that the opening hours are unavailable because
               they are provided above.
            """;

        var requestBody = new
        {
            model = _options.Model,
            instructions = systemInstructions,
            input = openAiMessages,
            stream = true
        };

        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody =
                await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "OpenAI request failed with status code {StatusCode}. Response: {ResponseBody}",
                (int)response.StatusCode,
                errorBody);

            throw new OpenAiServiceException(
                "The OpenAI service returned an unsuccessful response.",
                (int)response.StatusCode);
        }

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var reader = new StreamReader(stream);

        var completeResponse = new StringBuilder();

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var json = line["data: ".Length..];

            if (json == "[DONE]")
            {
                break;
            }

            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeProperty))
            {
                continue;
            }

            var eventType = typeProperty.GetString();

            if (eventType == "response.completed")
            {
                break;
            }

            if (eventType == "response.output_text.delta" &&
                root.TryGetProperty("delta", out var deltaProperty))
            {
                var delta = deltaProperty.GetString();

                if (!string.IsNullOrEmpty(delta))
                {
                    completeResponse.Append(delta);
                }
            }

            if (eventType == "error")
            {
                var errorMessage =
                    root.TryGetProperty("message", out var messageProperty)
                        ? messageProperty.GetString()
                        : "An unknown streaming error occurred.";

                throw new OpenAiServiceException(
                    errorMessage ?? "An unknown streaming error occurred.",
                    502);
            }
        }

        var finalResponse = completeResponse.ToString();

        string responseToReturn;

        if (_fallbackService.ShouldFallback(finalResponse))
        {
            _logger.LogInformation(
                "AI response triggered fallback detection");

            responseToReturn =
                _fallbackService.GetFallbackResponse();
        }
        else
        {
            responseToReturn = finalResponse;
        }

        if (!string.IsNullOrWhiteSpace(responseToReturn))
        {
            await _conversationService.AddMessageAsync(
                conversation.Id,
                "assistant",
                responseToReturn,
                cancellationToken);

            yield return responseToReturn;
        }

        _logger.LogInformation(
            "OpenAI streaming request completed successfully");
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "The OpenAI API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException(
                "The OpenAI base URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException(
                "The OpenAI model is not configured.");
        }
    }
}