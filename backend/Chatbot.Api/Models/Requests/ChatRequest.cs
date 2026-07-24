namespace Chatbot.Api.Models.Requests;

public class ChatRequest
{
    public List<ChatMessageRequest> Messages { get; set; } = [];
}