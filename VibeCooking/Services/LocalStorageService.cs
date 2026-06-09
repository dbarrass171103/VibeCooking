using VibeCooking.Models;
using VibeCooking.ViewModels;

namespace VibeCooking.Services;

// Implements local persistence using an injected IBarrel (backed by MonkeyCache in production).
public class LocalStorageService : ILocalStorageService
{
    private const string IngredientsKey = "ingredients_selections";
    private const string CustomIngredientsKey = "ingredients_custom";
    private const string RecipesKey = "saved_recipes";
    private const string CalendarRecipesKey = "calendar_recipes";

    private readonly IBarrel _barrel;

    public LocalStorageService(IBarrel barrel)
    {
        _barrel = barrel;
    }

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

            _barrel.Add(IngredientsKey, catalogSnapshots, TimeSpan.FromDays(365));
            _barrel.Add(CustomIngredientsKey, customSnapshots, TimeSpan.FromDays(365));
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
            if (!_barrel.IsExpired(IngredientsKey))
            {
                var snapshots = _barrel.Get<List<IngredientSnapshot>>(IngredientsKey);

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
            if (!_barrel.IsExpired(CustomIngredientsKey))
            {
                var customSnapshots = _barrel.Get<List<IngredientSnapshot>>(CustomIngredientsKey);

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

            _barrel.Add(RecipesKey, recipes, TimeSpan.FromDays(365));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Failed to save recipe: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public async Task SaveCalendarRecipeAsync(KeyValuePair<DateTime, string> calendarRecipe)
    {
        try
        {
            var current = await LoadCalendarRecipesAsync();

            if (current.ContainsKey(calendarRecipe.Key))
                current[calendarRecipe.Key].Add(calendarRecipe.Value);
            else
                current[calendarRecipe.Key] = [calendarRecipe.Value];

            _barrel.Add(CalendarRecipesKey, current, TimeSpan.FromDays(365));
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
            if (_barrel.IsExpired(CalendarRecipesKey))
                return new();

            var calendarRecipes = _barrel.Get<Dictionary<DateTime, List<string>>>(CalendarRecipesKey);
            return calendarRecipes
                .Where(x => x.Key.Date >= startDate.Date && x.Key.Date <= endDate.Date)
                .ToDictionary();
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
            if (_barrel.IsExpired(CalendarRecipesKey))
                return new();

            return _barrel.Get<Dictionary<DateTime, List<string>>>(CalendarRecipesKey) ?? new();
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

            _barrel.Add(CalendarRecipesKey, current, TimeSpan.FromDays(365));
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
            _barrel.Add(RecipesKey, recipes, TimeSpan.FromDays(365));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Failed to delete recipe: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private List<SavedRecipeModel> LoadRecipeList()
    {
        if (_barrel.IsExpired(RecipesKey))
            return new();

        return _barrel.Get<List<SavedRecipeModel>>(RecipesKey) ?? new();
    }
}

// Snapshot for persisting an ingredient's selected state and quantity.
// Internal so it is accessible in the test project (which links this file into the same assembly).
internal record IngredientSnapshot(string Name, IngredientCategory Category, bool IsSelected, string Quantity);
