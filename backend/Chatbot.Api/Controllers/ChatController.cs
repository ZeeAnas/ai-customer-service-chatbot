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
        if (request.Messages == null || request.Messages.Count == 0)
        {
            return BadRequest(new
            {
                error = "At least one message is required."
            });
        }

        foreach (var message in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(message.Role))
            {
                return BadRequest(new
                {
                    error = "Every message must have a role."
                });
            }
            if(string.IsNullOrWhiteSpace(message.Content))
            {
                return BadRequest(new{
                    error = "Every message must have content."
                });
            }
            if(message.Content.Length > 1000 )
            {
                return BadRequest(new
                {
                    error = "Message content cannot be longer than 1000 characters"
                });
            }
            
        }

        var reply = await _chatService.GetReplyAsync(
            request.Messages,
            cancellationToken
        );

        return Ok(new ChatResponse
        {
            Reply = reply
        });
    }
}