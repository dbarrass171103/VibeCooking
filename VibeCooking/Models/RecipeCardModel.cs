namespace VibeCooking.Models;
// Model for the initial 5 recipes that are returned by AI
public class RecipeCardModel
{
    public string RecipeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string MealType { get; set; } = string.Empty;
    public int CookTimeMinutes { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int Servings { get; set; }
    public List<string> Ingredients { get; set; } = new();
}