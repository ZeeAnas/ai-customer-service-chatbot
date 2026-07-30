using Chatbot.Api.Configuration;
using Chatbot.Api.Exceptions;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Requests;
using Chatbot.Api.Services;
using Chatbot.Api.Models.Entities;
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

        var chatService = CreateChatService(
            new HttpClient(),
            options);

        var messages = CreateMessages();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await foreach (var _ in chatService.StreamReplyAsync(
                    "test-session",
                                   messages,
                                   CancellationToken.None))
                {
                }
            });

        // Assert
        Assert.Contains(
            "API key is not configured",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
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

        var chatService = CreateChatService(
            new HttpClient(),
            options);

        var messages = CreateMessages();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await foreach (var _ in chatService.StreamReplyAsync(
                    "test-session",
                                   messages,
                                   CancellationToken.None))
                {
                }
            });

        // Assert
        Assert.Contains(
            "base URL is not configured",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
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

        var chatService = CreateChatService(
            new HttpClient(),
            options);

        var messages = CreateMessages();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await foreach (var _ in chatService.StreamReplyAsync(
                    "test-session",
                                   messages,
                                   CancellationToken.None))
                {
                }
            });

        // Assert
        Assert.Contains(
            "model is not configured",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StreamReplyAsync_WhenOpenAiReturnsError_ThrowsOpenAiServiceException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            new HttpResponseMessage(
                System.Net.HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("OpenAI error")
            });

        var httpClient = new HttpClient(handler);

        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.openai.com/v1/",
            Model = "gpt-5-mini"
        });

        var chatService = CreateChatService(
            httpClient,
            options);

        var messages = CreateMessages();

        // Act
        var exception = await Assert.ThrowsAsync<OpenAiServiceException>(
            async () =>
            {
                await foreach (var _ in chatService.StreamReplyAsync(
                    "test-session",
                                   messages,
                                   CancellationToken.None))
                {
                }
            });

        // Assert
        Assert.Contains(
            "unsuccessful response",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StreamReplyAsync_WhenOpenAiReturnsStreamingContent_YieldsCompleteText()
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
            });

        var httpClient = new HttpClient(handler);

        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.openai.com/v1/",
            Model = "gpt-5-mini"
        });

        var chatService = CreateChatService(
            httpClient,
            options);

        var messages = CreateMessages();
        var receivedChunks = new List<string>();

        // Act
        await foreach (var chunk in chatService.StreamReplyAsync(
                           "test-session",
                           messages,
                           CancellationToken.None))
        {
            receivedChunks.Add(chunk);
        }

        // Assert
        Assert.Single(receivedChunks);
        Assert.Equal("Hello there", receivedChunks[0]);
    }

    private static ChatService CreateChatService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options)
    {
        return new ChatService(
            httpClient,
            options,
            NullLogger<ChatService>.Instance,
            new TestPromptService(),
            new TestBusinessHoursService(),
            new TestFallbackService(),
            new TestConversationService());

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

    private sealed class TestBusinessHoursService : IBusinessHoursService
    {
        public bool IsOpenNow()
        {
            return true;
        }
        public string GetStatusMessage()
        {
            return "The business is currently open.";
        }

        public string GetWeeklyScheduleMessage()
        {
            return "Monday to Friday: 09:00–17:00.";
        }
    }

    private sealed class TestFallbackService : IFallbackService
    {
        public bool ShouldFallback(string response)
        {
            return false;
        }

        public string GetFallbackResponse()
        {
            return "Please contact a member of staff for assistance.";
        }
    }

    private sealed class TestConversationService : IConversationService
    {
        public Task<Conversation> GetOrCreateConversationAsync(
            string sessionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new Conversation
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow
            });
        }

        public Task AddMessageAsync(
            Guid conversationId,
            string role,
            string content,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<List<Message>> GetMessagesAsync(
            Guid conversationId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<Message>());
        }
        public Task<List<Message>> GetMessagesBySessionIdAsync(
    string sessionId,
    CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<Message>());
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