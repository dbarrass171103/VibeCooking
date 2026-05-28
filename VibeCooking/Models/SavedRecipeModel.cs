namespace VibeCooking.Models;

// Represents a recipe the user has saved. Wraps a RecipeCardModel but with data like image, liked status and saved date
public class SavedRecipeModel
{
    public RecipeCardModel Card { get; set; } = new();
    public string ImageBase64 { get; set; } = string.Empty;
    public bool IsLiked { get; set; } = false;
    public DateTime SavedAt { get; set; } = DateTime.Now;
}