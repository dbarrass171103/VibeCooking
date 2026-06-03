using VibeCooking.Models;
using VibeCooking.Services;

namespace VibeCooking.ViewModels;

public class MyRecipesViewModel : BaseViewModel
{
    private readonly ILocalStorageService _storage;
    private readonly RecipeChatViewModel _recipeChatViewModel;

    public List<SavedRecipeModel> Recipes { get; private set; } = new();
    public string SearchQuery { get; set; } = string.Empty;
    public bool IsLoading { get; private set; } = false;
    public string? ErrorMessage { get; private set; }

    /// Recipes filtered by the current search.
    public List<SavedRecipeModel> FilteredRecipes => string.IsNullOrWhiteSpace(SearchQuery)
        ? Recipes
        : Recipes.Where(r =>
            r.SaveName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
            r.Recipe.CuisineType.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
            r.Recipe.MealType.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
            r.Recipe.Difficulty.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
        .ToList();

    public MyRecipesViewModel(ILocalStorageService storage, RecipeChatViewModel recipeChatViewModel)
    {
        _storage = storage;
        _recipeChatViewModel = recipeChatViewModel;
    }

    public override async Task InitAsync()
    {
        await RefreshAsync();
    }

    // Reloads the recipe list from storage.
    public async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Recipes = await _storage.LoadRecipesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Deletes a recipe and refreshes the list.
    public async Task DeleteRecipeAsync(SavedRecipeModel recipe)
    {
        await _storage.DeleteRecipeAsync(recipe.Id);
        await RefreshAsync();
    }

    // Loads a saved recipe into the chat ViewModel so it can be opened and edited.
    public void OpenRecipe(SavedRecipeModel recipe)
    {
        _recipeChatViewModel.LoadRecipe(recipe.Recipe);
    }

    // Updates the photo on an existing saved recipe.
    public async Task UpdateRecipeImageAsync(Guid id, string? imageBase64, string? imageMimeType)
    {
        var recipe = Recipes.FirstOrDefault(r => r.Id == id);
        if (recipe is null)
            return;

        recipe.ImageBase64 = imageBase64;
        recipe.ImageMimeType = imageMimeType;

        await _storage.SaveRecipeAsync(recipe);
        await RefreshAsync();
    }
}