using System.ComponentModel.DataAnnotations;

namespace Chatbot.Api.Models.Requests;

public class CreateLeadRequest : IValidatableObject
{
    private string? _email;
    private string? _phone;

    [Required]
    [StringLength(200)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(254)]
    public string? Email
    {
        get => _email;
        set => _email = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    [Phone]
    [StringLength(30)]
    public string? Phone
    {
        get => _phone;
        set => _phone = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Range(
        typeof(bool),
        "true",
        "true",
        ErrorMessage = "Consent is required."
    )]
    public bool ConsentToContact { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext
    )
    {
        if (Email is null && Phone is null)
        {
            yield return new ValidationResult(
                "Either email or phone must be provided.",
                new[] { nameof(Email), nameof(Phone) }
            );
        }
    }
}