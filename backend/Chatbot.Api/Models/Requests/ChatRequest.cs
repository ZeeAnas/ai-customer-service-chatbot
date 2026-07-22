namespace Chatbot.Api.Models.Requests;

// This class represents the JSON sent by the frontend
public class ChatRequest
{
    public string Message {get; set;} = string.Empty;
}