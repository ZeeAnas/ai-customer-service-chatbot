using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Requests;
using Chatbot.Api.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IConversationService _conversationService;

    public ChatController(IChatService chatService, IConversationService conversationService)
    {
        _chatService = chatService;
        _conversationService = conversationService;
    }

    [HttpPost]
    public async Task SendMessage(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Messages == null || request.Messages.Count == 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;

            await Response.WriteAsJsonAsync(
                new
                {
                    error = "At least one message is required."
                },
                cancellationToken
            );

            return;
        }

        foreach (var message in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(message.Role))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;

                await Response.WriteAsJsonAsync(
                    new
                    {
                        error = "Every message must have a role."
                    },
                    cancellationToken
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(message.Content))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;

                await Response.WriteAsJsonAsync(
                    new
                    {
                        error = "Every message must have content."
                    },
                    cancellationToken
                );

                return;
            }

            if (
    message.Role.Equals(
        "user",
        StringComparison.OrdinalIgnoreCase
    ) &&
    message.Content.Length > 1000
)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;

                await Response.WriteAsJsonAsync(
                    new
                    {
                        error =
                            "User messages cannot be longer than 1000 characters."
                    },
                    cancellationToken
                );

                return;
            }
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/plain; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        HttpContext.Features
            .Get<IHttpResponseBodyFeature>()
            ?.DisableBuffering();

        await foreach (
            var chunk in _chatService.StreamReplyAsync(
                request.SessionId,
                request.Messages,
                cancellationToken
            )
        )
        {
            await Response.WriteAsync(
                chunk,
                cancellationToken
            );

            await Response.Body.FlushAsync(
                cancellationToken
            );
        }
    }

    [HttpGet("history/{sessionId}")]
    public async Task<IActionResult> GetHistory(
    string sessionId,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest(new
            {
                error = "Session ID cannot be empty."
            });
        }

        var messages =
            await _conversationService.GetMessagesBySessionIdAsync(
                sessionId,
                cancellationToken);

        var response = messages.Select(message => new
        {
            id = message.Id,
            role = message.Role,
            content = message.Content,
            createdAt = message.CreatedAt
        });

        return Ok(response);
    }
}