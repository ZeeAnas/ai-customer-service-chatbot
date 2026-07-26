using Chatbot.Api.Interfaces;
using Chatbot.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HandoffController : ControllerBase
{
    private readonly IHandoffService _handoffService;

    public HandoffController(
        IHandoffService handoffService
    )
    {
        _handoffService = handoffService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateHandoffRequest(
        [FromBody] HandoffRequestDto request,
        CancellationToken cancellationToken
    )
    {
        await _handoffService.ProcessAsync(
            request,
            cancellationToken
        );

        return Ok(new
        {
            message = "Your request has been received."
        });
    }
}