using VibeCooking.Models;
using VibeCooking.ViewModels;

namespace VibeCooking.Services;

// Defines local persistence operations for the app.
public interface ILocalStorageService
{
    /// <summary>
    /// Persists the selected state and quantities of all ingredients
    /// (catalog + custom) from the given ViewModel to local storage.
    /// </summary>
    Task SaveIngredientsAsync(IngredientsViewModel viewModel);

    /// <summary>
    /// Restores ingredient selections and custom ingredients from local
    /// storage into the given ViewModel. No-ops if nothing has been saved yet.
    /// </summary>
    Task LoadIngredientsAsync(IngredientsViewModel viewModel);
}