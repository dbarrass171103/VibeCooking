namespace VibeCooking.Models;

// Represents a single message in the recipe chat conversation history.
public class ChatMessageModel
{
    public string Role { get; set; } = string.Empty;   // "user" or "assistant"
    public string Content { get; set; } = string.Empty;

    public bool IsUser => Role == "user";
}