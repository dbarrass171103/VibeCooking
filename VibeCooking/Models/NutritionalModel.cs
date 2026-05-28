namespace VibeCooking.Models;

// Represents the nutritional information per serving for a recipe.
public class NutritionalModel
{
    public int CaloriesKcal { get; set; }
    public float ProteinG { get; set; }
    public float CarbsG { get; set; }
    public float FatG { get; set; }
    public float FibreG { get; set; }
    public float SugarG { get; set; }
    public float SaltG { get; set; }
}