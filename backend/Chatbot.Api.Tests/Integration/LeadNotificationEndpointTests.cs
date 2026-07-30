using System.Net;
using System.Net.Http.Json;
using Chatbot.Api.Models.Responses;

namespace Chatbot.Api.Tests.Integration;

public sealed class LeadNotificationEndpointTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeLeadNotificationService
        _notificationService;

    public LeadNotificationEndpointTests(
        CustomWebApplicationFactory factory
    )
    {
        _client = factory.CreateClient();
        _notificationService =
            factory.LeadNotificationService;

        _notificationService.Reset();
    }

    [Fact]
    public async Task CreateLead_WithValidRequest_SavesLeadAndNotifiesStaff()
    {
        // Arrange
        var sessionId =
            $"lead-notification-test-{Guid.NewGuid()}";

        var chatRequest = new
        {
            sessionId,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "I would like to be contacted."
                }
            }
        };

        var chatResponse = await _client.PostAsJsonAsync(
            "/api/chat",
            chatRequest
        );

        Assert.Equal(
            HttpStatusCode.OK,
            chatResponse.StatusCode
        );

        var leadRequest = new
        {
            sessionId,
            name = "Integration Test Customer",
            email = "customer@example.com",
            phone = (string?)null,
            message = "Please contact me.",
            consentToContact = true
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/leads",
            leadRequest
        );

        var createdLead =
            await response.Content
                .ReadFromJsonAsync<LeadResponse>();

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );

        Assert.NotNull(createdLead);
        Assert.True(createdLead.Id > 0);

        Assert.Equal(
            1,
            _notificationService.CallCount
        );

        Assert.NotNull(
            _notificationService.LastLead
        );

        Assert.Equal(
            createdLead.Id,
            _notificationService.LastLead.Id
        );

        Assert.Equal(
            "Integration Test Customer",
            _notificationService.LastLead.Name
        );

        Assert.Equal(
            "customer@example.com",
            _notificationService.LastLead.Email
        );
    }
    [Fact]
public async Task CreateLead_WhenNotificationFails_StillReturnsCreated()
{
    // Arrange
    var sessionId =
        $"notification-failure-test-{Guid.NewGuid()}";

    var chatRequest = new
    {
        sessionId,
        messages = new[]
        {
            new
            {
                role = "user",
                content = "Please ask someone to contact me."
            }
        }
    };

    var chatResponse = await _client.PostAsJsonAsync(
        "/api/chat",
        chatRequest
    );

    Assert.Equal(
        HttpStatusCode.OK,
        chatResponse.StatusCode
    );

    _notificationService.ShouldFail = true;

    var leadRequest = new
    {
        sessionId,
        name = "Notification Failure Customer",
        email = "failure-test@example.com",
        phone = (string?)null,
        message = "Please contact me.",
        consentToContact = true
    };

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/leads",
        leadRequest
    );

    // Assert
    Assert.Equal(
        HttpStatusCode.Created,
        response.StatusCode
    );

    var createdLead =
        await response.Content
            .ReadFromJsonAsync<LeadResponse>();

    Assert.NotNull(createdLead);
    Assert.True(createdLead.Id > 0);

    Assert.Equal(
        1,
        _notificationService.CallCount
    );

    var persistedResponse = await _client.GetAsync(
        $"/api/leads/{createdLead.Id}"
    );

    Assert.Equal(
        HttpStatusCode.OK,
        persistedResponse.StatusCode
    );
}
}