using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Enums;
using Chatbot.Api.Models.Requests;
using Chatbot.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(LeadResponse),
        StatusCodes.Status201Created
    )]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest
    )]
    [ProducesResponseType(
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        StatusCodes.Status409Conflict
    )]
    public async Task<ActionResult<LeadResponse>> Create(
        [FromBody] CreateLeadRequest request,
        CancellationToken cancellationToken
    )
    {
        var lead = await _leadService.CreateAsync(
            request,
            cancellationToken
        );

        return CreatedAtAction(
            nameof(GetById),
            new { id = lead.Id },
            lead
        );
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<LeadResponse>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest
    )]
    public async Task<ActionResult<IReadOnlyList<LeadResponse>>> GetAll(
        [FromQuery] LeadStatus? status,
        CancellationToken cancellationToken
    )
    {
        if (
            status.HasValue &&
            !Enum.IsDefined(status.Value)
        )
        {
            ModelState.AddModelError(
                nameof(status),
                "The selected lead status is invalid."
            );

            return ValidationProblem(ModelState);
        }

        var leads = await _leadService.GetAllAsync(
            status,
            cancellationToken
        );

        return Ok(leads);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(LeadResponse),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(
        StatusCodes.Status404NotFound
    )]
    public async Task<ActionResult<LeadResponse>> GetById(
        int id,
        CancellationToken cancellationToken
    )
    {
        var lead = await _leadService.GetByIdAsync(
            id,
            cancellationToken
        );

        return Ok(lead);
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(
        typeof(LeadResponse),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest
    )]
    [ProducesResponseType(
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        StatusCodes.Status409Conflict
    )]
    public async Task<ActionResult<LeadResponse>> Update(
        int id,
        [FromBody] UpdateLeadRequest request,
        CancellationToken cancellationToken
    )
    {
        var lead = await _leadService.UpdateAsync(
            id,
            request,
            cancellationToken
        );

        return Ok(lead);
    }
}