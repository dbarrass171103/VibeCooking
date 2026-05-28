namespace VibeCooking.Models;

// Full detailed recipe returned by the AI in the second generation step,
public class RecipeOutputModel
{
    public string RecipeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string MealType { get; set; } = string.Empty;
    public int CookTimeMinutes { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int Servings { get; set; }
    public List<IngredientModel> Ingredients { get; set; } = new();
    public List<string> Equipment { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
    public NutritionalModel NutritionPerServing { get; set; } = new();
    public string StorageAdvice { get; set; } = string.Empty;
    public List<string> Substitutions { get; set; } = new();
}