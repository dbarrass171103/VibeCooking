using System.Text;

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
		var sb = new StringBuilder();

		sb.AppendLine($"🍽 {SaveName}");
		sb.AppendLine(new string('=', 40));
		sb.AppendLine();

		sb.AppendLine($"📌 {Recipe.RecipeName}");
		sb.AppendLine(Recipe.Description);
		sb.AppendLine();

		sb.AppendLine($"Cuisine: {Recipe.CuisineType}");
		sb.AppendLine($"Difficulty: {Recipe.Difficulty}");
		sb.AppendLine($"Meal Type: {Recipe.MealType}");
		sb.AppendLine();

		sb.AppendLine($"⏱ Prep Time: {Recipe.PrepTimeMinutes} min");
		sb.AppendLine($"🔥 Cook Time: {Recipe.CookTimeMinutes} min");
		sb.AppendLine($"🍽 Servings: {Recipe.Servings}");
		sb.AppendLine();

		sb.AppendLine("🧾 Ingredients:");

		foreach (var ingredient in Recipe.Ingredients)
		{
			var optionalTag = ingredient.IsOptional ? " (optional)" : "";
			sb.AppendLine($"- {ingredient.Quantity} {ingredient.Name}{optionalTag}");
		}

		sb.AppendLine();

		sb.AppendLine("🍳 Equipment:");

		foreach (var item in Recipe.Equipment)
		{
			sb.AppendLine($"- {item}");
		}

		sb.AppendLine();

		sb.AppendLine("👨‍🍳 Instructions:");

		for (int i = 0; i < Recipe.Instructions.Count; i++)
		{
			sb.AppendLine($"{i + 1}. {Recipe.Instructions[i]}");
		}

		sb.AppendLine();

		sb.AppendLine("📊 Nutrition (per serving):");
		sb.AppendLine($"- Calories: {Recipe.NutritionPerServing.CaloriesKcal} kcal");
		sb.AppendLine($"- Protein: {Recipe.NutritionPerServing.ProteinG} g");
		sb.AppendLine($"- Carbs: {Recipe.NutritionPerServing.CarbsG} g");
		sb.AppendLine($"- Fat: {Recipe.NutritionPerServing.FatG} g");
		sb.AppendLine($"- Fibre: {Recipe.NutritionPerServing.FibreG} g");
		sb.AppendLine($"- Sugar: {Recipe.NutritionPerServing.SugarG} g");
		sb.AppendLine($"- Salt: {Recipe.NutritionPerServing.SaltG} g");
		sb.AppendLine();

		if (!string.IsNullOrWhiteSpace(Recipe.StorageAdvice))
		{
			sb.AppendLine("🧊 Storage Advice:");
			sb.AppendLine(Recipe.StorageAdvice);
			sb.AppendLine();
		}

		if (Recipe.Substitutions?.Count > 0)
		{
			sb.AppendLine("🔁 Substitutions:");

			foreach (var sub in Recipe.Substitutions)
			{
				sb.AppendLine($"- {sub}");
			}

			sb.AppendLine();
		}

		return sb.ToString().TrimEnd();
	}
}