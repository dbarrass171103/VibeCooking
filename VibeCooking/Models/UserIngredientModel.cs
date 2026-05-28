namespace VibeCooking.Models;

// Represents a single ingredient in the user's catalog.
public class UserIngredient
{
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = false;
    public bool IsBinary { get; set; } = false;
    public string Quantity { get; set; } = string.Empty;
    public bool IsCustom { get; set; } = false;
}