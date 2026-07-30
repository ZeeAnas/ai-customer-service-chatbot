using System.ComponentModel.DataAnnotations;
using Chatbot.Api.Models.Enums;

namespace Chatbot.Api.Models.Requests;

public class UpdateLeadRequest : IValidatableObject
{
    [Required]
    public LeadStatus? Status { get; set; }

    [MaxLength(2000)]
    public string? StaffNotes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext
    )
    {
        if (
            Status.HasValue &&
            !Enum.IsDefined(Status.Value)
        )
        {
            yield return new ValidationResult(
                "The selected lead status is invalid.",
                new[] { nameof(Status) }
            );
        }
    }
}