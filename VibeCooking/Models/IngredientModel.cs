namespace VibeCooking.Models;

// Represents a single ingredient in a full recipe.
public class IngredientModel
{
    public string Name { get; set; } = string.Empty;

    // The quantity needed e.g. "2", "100g", "a handful"
    public string Quantity { get; set; } = string.Empty;

    // Whether this ingredient is optional in the recipe
    public bool IsOptional { get; set; } = false;
}