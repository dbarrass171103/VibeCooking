namespace VibeCooking.Models;

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
    public List<IngredientItem> Ingredients { get; set; } = new();
    public List<string> Equipment { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
    public NutritionInfo NutritionPerServing { get; set; } = new();
    public string StorageAdvice { get; set; } = string.Empty;
    public List<string> Substitutions { get; set; } = new();
}

public class IngredientItem
{
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public bool IsOptional { get; set; } = false;
}

public class NutritionInfo
{
    public int CaloriesKcal { get; set; }
    public float ProteinG { get; set; }
    public float CarbsG { get; set; }
    public float FatG { get; set; }
    public float FibreG { get; set; }
    public float SugarG { get; set; }
    public float SaltG { get; set; }
}
