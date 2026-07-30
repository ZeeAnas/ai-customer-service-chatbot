using Chatbot.Api.Models.Enums;
using Chatbot.Api.Models.Requests;
using Chatbot.Api.Models.Responses;

namespace Chatbot.Api.Interfaces;

public interface ILeadService
{
    Task<LeadResponse> CreateAsync(
        CreateLeadRequest request,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<LeadResponse>> GetAllAsync(
        LeadStatus? status,
        CancellationToken cancellationToken
    );

    Task<LeadResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken
    );

    Task<LeadResponse> UpdateAsync(
        int id,
        UpdateLeadRequest request,
        CancellationToken cancellationToken
    );
}