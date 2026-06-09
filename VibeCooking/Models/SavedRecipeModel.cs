namespace VibeCooking.Models;

// Represents a recipe the user has saved. Stores the full recipe for editing and the card for summary display.
public class SavedRecipeModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SaveName { get; set; } = string.Empty;
    public RecipeOutputModel Recipe { get; set; } = new();
    public RecipeCardModel Card { get; set; } = new();
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public string? ImageBase64 { get; set; }
    public string? ImageMimeType { get; set; }

	public override string ToString()
	{
        string ingredientsJoined = string.Join(',', Recipe.Ingredients.Select(x => x.Name));
        string instructionsJoined = string.Join(Environment.NewLine, Recipe.Instructions);

        return $"""
            Name: {SaveName},
            Prep Time: {Recipe.PrepTimeMinutes} mins,
            Servings: {Recipe.Servings},
            Prep Time: {Recipe.PrepTimeMinutes} mins,
            Ingredients: {ingredientsJoined},

            ========================================

            Instructions: 
            
            {instructionsJoined}
        """;
	}
}