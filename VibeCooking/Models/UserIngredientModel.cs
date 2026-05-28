namespace VibeCooking.Models;

public enum IngredientCategory
{
    Meat,
    Vegetables,
    Fruit,
    Dairy,
    Grains,
    Seasonings,
    OilsAndCondiments,
    Misc
}

// Represents a single ingredient in the user's pantry catalog.
public class UserIngredient
{
    public string Name { get; set; } = string.Empty;
    public IngredientCategory Category { get; set; } = IngredientCategory.Misc;
    public bool IsSelected { get; set; } = false;
    public bool IsBinary { get; set; } = false;
    public string Quantity { get; set; } = string.Empty;
    public bool IsCustom { get; set; } = false;
}