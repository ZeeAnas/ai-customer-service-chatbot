using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Requests;
using Chatbot.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> SendMessage(
    [FromBody] ChatRequest request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                error = "Message is required."
            });
        }

        if (request.Message.Length > 1000)
        {
            return BadRequest(new
            {
                error = "Message cannot be longer than 1000 characters."
            });
        }

        var reply = await _chatService.GetReplyAsync(
            request.Message.Trim(),
            cancellationToken
        );

        return Ok(new ChatResponse
        {
            Reply = reply
        });
    }
}