namespace VibeCooking.Models;

// Represents the AI's structured response during a recipe chat turn.
public class ChatResponseModel
{
    public string Reply { get; set; } = string.Empty; // The plain text reply
    public RecipeOutputModel Recipe { get; set; } = new();
}