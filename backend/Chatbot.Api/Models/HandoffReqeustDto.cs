using System.ComponentModel.DataAnnotations;

namespace Chatbot.Api.Models;

public sealed class HandoffRequestDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "Name must be between 2 and 100 characters."
    )]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(
        254,
        ErrorMessage = "Email cannot exceed 254 characters."
    )]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(
        30,
        ErrorMessage = "Phone number cannot exceed 30 characters."
    )]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(
        2000,
        MinimumLength = 5,
        ErrorMessage = "Message must be between 5 and 2000 characters."
    )]
    public string Message { get; set; } = string.Empty;
}