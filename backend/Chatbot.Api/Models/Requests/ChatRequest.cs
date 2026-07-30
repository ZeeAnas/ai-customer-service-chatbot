using System.ComponentModel.DataAnnotations;

namespace Chatbot.Api.Models.Requests;

public class ChatRequest
{
    [Required]
    [StringLength(100)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<ChatMessageRequest> Messages { get; set; } = [];
}