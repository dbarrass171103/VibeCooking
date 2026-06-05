using VibeCooking.Models;
using VibeCooking.ViewModels;

namespace VibeCooking.Services;

// Defines local persistence operations for the app.
public interface ILocalStorageService
{
    // Persists the selected state and quantities of all ingredients (catalog + custom) from the given ViewModel to local storage.
    Task SaveIngredientsAsync(IngredientsViewModel viewModel);


    // Restores ingredient selections and custom ingredients from local storage into the given ViewModel. No-ops if nothing has been saved yet.
    Task LoadIngredientsAsync(IngredientsViewModel viewModel);

    // Saves a recipe. If a recipe with the same Id already exists it is overwritten.
    Task SaveRecipeAsync(SavedRecipeModel recipe);

    // Loadsall saved recipes, ordered by most recently saved first.
    Task<List<SavedRecipeModel>> LoadRecipesAsync();

    // Deletes a saved recipe by Id.
    Task DeleteRecipeAsync(Guid id);

    public Task<Dictionary<DateTime, List<string>>> LoadCalendarRecipesAsync(DateTime startDate, DateTime endDate);
    public Task<Dictionary<DateTime, List<string>>> LoadCalendarRecipesAsync();

	public Task SaveCalendarRecipeAsync(KeyValuePair<DateTime, string> calendarRecipe);
}