using MonkeyCache.FileStore;
using VibeCooking.Models;
using VibeCooking.ViewModels;

namespace VibeCooking.Services;

// Implements local persistence using MonkeyCache.FileStore.
public class LocalStorageService : ILocalStorageService
{
    private const string IngredientsKey = "ingredients_selections";
    private const string CustomIngredientsKey = "ingredients_custom";
    private const string RecipesKey = "saved_recipes";
    private const string CalendarRecipesKey = "calendar_recipes";

    // Snapshot for persisting an ingredient's selected state and quantity.
    private record IngredientSnapshot(string Name, IngredientCategory Category, bool IsSelected, string Quantity);

    public Task SaveIngredientsAsync(IngredientsViewModel viewModel)
    {
        try
        {
            var catalogSnapshots = viewModel.IngredientsByCategory.Values
                .SelectMany(x => x)
                .Where(x => !x.IsCustom)
                .Select(i => new IngredientSnapshot(i.Name, i.Category, i.IsSelected, i.Quantity))
                .ToList();

            var customSnapshots = viewModel.IngredientsByCategory.Values
                .SelectMany(x => x)
                .Where(x => x.IsCustom)
                .Select(i => new IngredientSnapshot(i.Name, i.Category, i.IsSelected, i.Quantity))
                .ToList();

            Barrel.Current.Add(IngredientsKey, catalogSnapshots, TimeSpan.FromDays(365));
            Barrel.Current.Add(CustomIngredientsKey, customSnapshots, TimeSpan.FromDays(365));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Failed to save ingredients: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public Task LoadIngredientsAsync(IngredientsViewModel viewModel)
    {
        try
        {
            if (!Barrel.Current.IsExpired(IngredientsKey))
            {
                var snapshots = Barrel.Current.Get<List<IngredientSnapshot>>(IngredientsKey);

                if (snapshots is not null)
                {
                    var lookup = snapshots
                        .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    foreach (var ingredient in viewModel.IngredientsByCategory.Values.SelectMany(x => x))
                    {
                        if (lookup.TryGetValue(ingredient.Name, out var snapshot))
                        {
                            ingredient.IsSelected = snapshot.IsSelected;
                            ingredient.Quantity = snapshot.Quantity;
                        }
                    }
                }
            }

            // Reconstruct custom ingredients and insert them into the correct category.
            if (!Barrel.Current.IsExpired(CustomIngredientsKey))
            {
                var customSnapshots = Barrel.Current.Get<List<IngredientSnapshot>>(CustomIngredientsKey);

                if (customSnapshots is not null)
                {
                    foreach (var snapshot in customSnapshots)
                    {
                    // Avoid re-adding if already present
                        bool alreadyExists = viewModel.IngredientsByCategory[snapshot.Category]
                            .Any(x => x.Name.Equals(snapshot.Name, StringComparison.OrdinalIgnoreCase));

                        if (!alreadyExists)
                        {
                            viewModel.IngredientsByCategory[snapshot.Category].Add(new UserIngredient
                            {
                                Name = snapshot.Name,
                                Category = snapshot.Category,
                                IsSelected = snapshot.IsSelected,
                                Quantity = snapshot.Quantity,
                                IsBinary = snapshot.Category is IngredientCategory.Seasonings or IngredientCategory.OilsAndCondiments,
                                IsCustom = true
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Failed to load ingredients: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    // Recipe Saving

    public Task SaveRecipeAsync(SavedRecipeModel recipe)
    {
        try
        {
            var recipes = LoadRecipeList();

            int existing = recipes.FindIndex(r => r.Id == recipe.Id);
            if (existing >= 0)
                recipes[existing] = recipe;
            else
                recipes.Add(recipe);

            Barrel.Current.Add(RecipesKey, recipes, TimeSpan.FromDays(365));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Failed to save recipe: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public async Task SaveCalendarRecipeAsync(KeyValuePair<DateTime, string> calendarRecipe)
    {
        //We need to load the current saved recipes, delete the entry, and rewrite all as a day might have had a change of recipe.
        try
        {
            var currentCalendarRecipes = await LoadCalendarRecipesAsync();

            if (currentCalendarRecipes.ContainsKey(calendarRecipe.Key))
            {
                currentCalendarRecipes[calendarRecipe.Key].Add(calendarRecipe.Value);
            }
            else
            {
                currentCalendarRecipes[calendarRecipe.Key] = [calendarRecipe.Value];
			}

            Barrel.Current.Add(CalendarRecipesKey, currentCalendarRecipes, TimeSpan.FromDays(365));
        }
        catch (Exception ex)
        {
			Console.WriteLine($"[LocalStorageService] Failed to save calendar recipe: {ex.Message}");
		}
	}

    public async Task<Dictionary<DateTime, List<string>>> LoadCalendarRecipesAsync(DateTime startDate, DateTime endDate)
    {
		try
		{
            if (Barrel.Current.IsExpired(CalendarRecipesKey))
            {
                return new();
            }
            else
            {
                var calendarRecipes = Barrel.Current.Get<Dictionary<DateTime, List<string>>>(CalendarRecipesKey);

                return calendarRecipes.Where(x => x.Key.Date >= startDate.Date && x.Key.Date <= endDate.Date).ToDictionary();
            }
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[LocalStorageService] Failed to load recipes: {ex.Message}");
            return new();
		}
	}

	public async Task<Dictionary<DateTime, List<string>>> LoadCalendarRecipesAsync()
	{
		try
		{
			if (Barrel.Current.IsExpired(CalendarRecipesKey))
			{
				return new();
			}
			else
			{
				var calendarRecipes = Barrel.Current.Get<Dictionary<DateTime, List<string>>>(CalendarRecipesKey);
                return calendarRecipes;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[LocalStorageService] Failed to load recipes: {ex.Message}");
			return new();
		}
	}

	public async Task RemoveCalendarRecipeAsync(DateTime date, string recipeId)
	{
		try
		{
			var current = await LoadCalendarRecipesAsync();

			if (current.TryGetValue(date, out var ids))
			{
				ids.Remove(recipeId);
				if (ids.Count == 0)
					current.Remove(date);
			}

			Barrel.Current.Add(CalendarRecipesKey, current, TimeSpan.FromDays(365));
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[LocalStorageService] Failed to remove calendar recipe: {ex.Message}");
		}
	}

	public Task<List<SavedRecipeModel>> LoadRecipesAsync()
    {
        try
        {
            var recipes = LoadRecipeList()
                .OrderByDescending(r => r.SavedAt)
                .ToList();

            return Task.FromResult(recipes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Failed to load recipes: {ex.Message}");
            return Task.FromResult(new List<SavedRecipeModel>());
        }
    }

    public Task DeleteRecipeAsync(Guid id)
    {
        try
        {
            var recipes = LoadRecipeList();
            recipes.RemoveAll(r => r.Id == id);
            Barrel.Current.Add(RecipesKey, recipes, TimeSpan.FromDays(365));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Failed to delete recipe: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private List<SavedRecipeModel> LoadRecipeList()
    {
        if (Barrel.Current.IsExpired(RecipesKey))
            return new();

        return Barrel.Current.Get<List<SavedRecipeModel>>(RecipesKey) ?? new();
    }
}