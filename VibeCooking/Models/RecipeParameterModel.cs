namespace VibeCooking.Models;

// Holds user selected parameters. Passes to the API to build prompt to be sent to AI
 
public class RecipeParameterModel
{
    public List<string> Ingredients { get; set; } = new();
    public List<string> Allergies { get; set; } = new();
    public string Difficulty { get; set; } = "Any";
    public string MealType { get; set; } = "Any";
    public string CuisineType { get; set; } = "Any";
    public int CookTimeMinutes { get; set; } = 30;
    public bool AnyCookTime { get; set; } = false;
    public string IngredientUsage { get; set; } = "Mainly ingredients I have";
    public int CardCount { get; set; } = 3;
    public int Servings { get; set; } = 4;
    public bool Vegetarian { get; set; } = false;
    public bool Vegan { get; set; } = false;
    public bool GlutenFree { get; set; } = false;
    public bool DairyFree { get; set; } = false;
    public string AdditionalNotes { get; set; } = string.Empty;
}