using Chatbot.Api.Configuration;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Requests;
using Chatbot.Api.Services;
using Chatbot.Api.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Chatbot.Api.Tests.Services;

public class ChatServiceTests
{
    [Fact]
    public async Task StreamReplyAsync_WhenApiKeyIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = string.Empty,
            BaseUrl = "https://api.openai.com/v1/",
            Model = "gpt-5-mini"
        });

        var chatService = new ChatService(
            new HttpClient(),
            options,
            NullLogger<ChatService>.Instance,
            new TestPromptService()
        );

        var messages = CreateMessages();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await foreach (var _ in chatService.StreamReplyAsync(
                                   messages,
                                   CancellationToken.None))
                {
                }
            }
        );

        // Assert
        Assert.Contains(
            "API key is not configured",
            exception.Message,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task StreamReplyAsync_WhenBaseUrlIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = "test-api-key",
            BaseUrl = string.Empty,
            Model = "gpt-5-mini"
        });

        var chatService = new ChatService(
            new HttpClient(),
            options,
            NullLogger<ChatService>.Instance,
            new TestPromptService()
        );

        var messages = CreateMessages();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await foreach (var _ in chatService.StreamReplyAsync(
                                   messages,
                                   CancellationToken.None))
                {
                }
            }
        );

        // Assert
        Assert.Contains(
            "base URL is not configured",
            exception.Message,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task StreamReplyAsync_WhenModelIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.openai.com/v1/",
            Model = string.Empty
        });

        var chatService = new ChatService(
            new HttpClient(),
            options,
            NullLogger<ChatService>.Instance,
            new TestPromptService()
        );

        var messages = CreateMessages();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await foreach (var _ in chatService.StreamReplyAsync(
                                   messages,
                                   CancellationToken.None))
                {
                }
            }
        );

        // Assert
        Assert.Contains(
            "model is not configured",
            exception.Message,
            StringComparison.OrdinalIgnoreCase
        );
    }

    
[Fact]
public async Task StreamReplyAsync_WhenOpenAiReturnsError_ThrowsOpenAiServiceException()
{
    // Arrange
    var handler = new TestHttpMessageHandler(
        new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("OpenAI error")
        }
    );

    var httpClient = new HttpClient(handler);

    var options = Options.Create(new OpenAiOptions
    {
        ApiKey = "test-api-key",
        BaseUrl = "https://api.openai.com/v1/",
        Model = "gpt-5-mini"
    });

    var chatService = new ChatService(
        httpClient,
        options,
        NullLogger<ChatService>.Instance,
        new TestPromptService()
    );

    var messages = CreateMessages();

    // Act
    var exception = await Assert.ThrowsAsync<OpenAiServiceException>(
        async () =>
        {
            await foreach (var _ in chatService.StreamReplyAsync(
                               messages,
                               CancellationToken.None))
            {
            }
        }
    );

    // Assert
    Assert.Contains(
        "unsuccessful response",
        exception.Message,
        StringComparison.OrdinalIgnoreCase
    );
}

[Fact]
public async Task StreamReplyAsync_WhenOpenAiReturnsStreamingContent_YieldsText()
{
    // Arrange
    const string streamingResponse =
        "data: {\"type\":\"response.output_text.delta\",\"delta\":\"Hello\"}\n\n" +
        "data: {\"type\":\"response.output_text.delta\",\"delta\":\" there\"}\n\n" +
        "data: [DONE]\n\n";

    var handler = new TestHttpMessageHandler(
        new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(streamingResponse)
        }
    );

    var httpClient = new HttpClient(handler);

    var options = Options.Create(new OpenAiOptions
    {
        ApiKey = "test-api-key",
        BaseUrl = "https://api.openai.com/v1/",
        Model = "gpt-5-mini"
    });

    var chatService = new ChatService(
        httpClient,
        options,
        NullLogger<ChatService>.Instance,
        new TestPromptService()
    );

    var messages = CreateMessages();
    var receivedChunks = new List<string>();

    // Act
    await foreach (var chunk in chatService.StreamReplyAsync(
                       messages,
                       CancellationToken.None))
    {
        receivedChunks.Add(chunk);
    }

    // Assert
    Assert.Equal(["Hello", " there"], receivedChunks);
    Assert.Equal("Hello there", string.Concat(receivedChunks));
}

    private static List<ChatMessageRequest> CreateMessages()
    {
        return
        [
            new ChatMessageRequest
            {
                Role = "user",
                Content = "What are your opening hours?"
            }
        ];
    }

    private sealed class TestPromptService : IPromptService
    {
        public string GetSystemPrompt()
        {
            return "You are a test customer-service assistant.";
        }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public TestHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
}