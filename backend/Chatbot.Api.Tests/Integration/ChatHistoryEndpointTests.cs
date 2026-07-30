using System.Net;
using System.Text.Json;


namespace Chatbot.Api.Tests.Integration;

public class ChatHistoryEndpointTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChatHistoryEndpointTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnknownSession_ReturnsOkWithEmptyHistory()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync(
            $"/api/chat/history/{sessionId}"
        );

        var json = await response.Content
            .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        Assert.Equal(
            JsonValueKind.Array,
            document.RootElement.ValueKind
        );

        Assert.Equal(
            0,
            document.RootElement.GetArrayLength()
        );
    }

    [Fact]
public async Task PostChat_ShouldPersistMessagesInHistory()
{
    // Arrange
    var sessionId =
        $"integration-test-{Guid.NewGuid()}";

    var request = new
    {
        sessionId,
        messages = new[]
        {
            new
            {
                role = "user",
                content = "What time do you open?"
            }
        }
    };

    // Act: send a chat message
    var postResponse = await _client.PostAsJsonAsync(
        "/api/chat",
        request
    );

    var streamedResponse =
        await postResponse.Content.ReadAsStringAsync();

    // Assert: POST succeeded
    Assert.Equal(
        HttpStatusCode.OK,
        postResponse.StatusCode
    );

    Assert.Equal(
        "This is a fake integration-test response.",
        streamedResponse
    );

    // Act: retrieve the stored history
    var historyResponse = await _client.GetAsync(
        $"/api/chat/history/{sessionId}"
    );

    var historyJson =
        await historyResponse.Content.ReadAsStringAsync();

    using var document =
        JsonDocument.Parse(historyJson);

    var messages = document.RootElement;

    // Assert: history contains user + assistant
    Assert.Equal(
        HttpStatusCode.OK,
        historyResponse.StatusCode
    );

    Assert.Equal(
        JsonValueKind.Array,
        messages.ValueKind
    );

    Assert.Equal(
        2,
        messages.GetArrayLength()
    );

    var userMessage = messages[0];
    var assistantMessage = messages[1];

    Assert.Equal(
        "user",
        userMessage.GetProperty("role").GetString()
    );

    Assert.Equal(
        "What time do you open?",
        userMessage.GetProperty("content").GetString()
    );

    Assert.Equal(
        "assistant",
        assistantMessage.GetProperty("role").GetString()
    );

    Assert.Equal(
        "This is a fake integration-test response.",
        assistantMessage
            .GetProperty("content")
            .GetString()
    );
}
[Fact]
public async Task PostChat_WithEmptyMessages_ShouldReturnBadRequest()
{
    // Arrange
    var request = new
    {
        sessionId = $"integration-test-{Guid.NewGuid()}",
        messages = Array.Empty<object>()
    };

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/chat",
        request
    );

    var responseBody =
        await response.Content.ReadAsStringAsync();

    // Assert
    Assert.Equal(
        HttpStatusCode.BadRequest,
        response.StatusCode
    );

    Assert.False(
        string.IsNullOrWhiteSpace(responseBody)
    );
}
}