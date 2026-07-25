using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
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

    public ChatService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<ChatService> logger,
        IPromptService promptService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _promptService = promptService;
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        List<ChatMessageRequest> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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

        var requestBody = new
        {
            model = _options.Model,
            instructions = _promptService.GetSystemPrompt(),
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

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

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
                _logger.LogInformation(
                    "OpenAI streaming request completed successfully");

                yield break;
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
                _logger.LogInformation(
                    "OpenAI streaming request completed successfully");

                yield break;
            }

            if (eventType == "response.output_text.delta" &&
                root.TryGetProperty("delta", out var deltaProperty))
            {
                var delta = deltaProperty.GetString();

                if (!string.IsNullOrEmpty(delta))
                {
                    yield return delta;
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