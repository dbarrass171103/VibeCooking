using VibeCooking.Models;
using VibeCooking.Services;

namespace VibeCooking.ViewModels;

public class RecipeChatViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly RecipeGenerationViewModel _recipeGenerationViewModel;
    private readonly ILocalStorageService _storage;

    // The live recipe — starts as the generated recipe and updates with each chat turn
    public RecipeOutputModel? CurrentRecipe { get; private set; }
    // Full conversation history
    public List<ChatMessageModel> Messages { get; private set; } = new();

    public bool IsSending { get; private set; } = false;
    public string? ErrorMessage { get; private set; }

    // True once the recipe has been loaded
    public bool IsReady => CurrentRecipe is not null;

    // When true, InitAsync will not overwrite the recipe loaded via LoadRecipe.
    // Set by LoadRecipe, cleared by InitAsync after loading from generation.
    private bool _loadedFromSaved = false;

    public RecipeChatViewModel(
        IApiService apiService, RecipeGenerationViewModel recipeGenerationViewModel,
        ILocalStorageService storage)
    {
        _apiService = apiService;
        _recipeGenerationViewModel = recipeGenerationViewModel;
        _storage = storage;
    }

    // Loads the generated recipe from RecipeGenerationViewModel. Skipped if a saved recipe was loaded via LoadRecipe.
    public override Task InitAsync()
    {
        if (_loadedFromSaved)
        {
            _loadedFromSaved = false;
            return Task.CompletedTask;
        }


        CurrentRecipe = _recipeGenerationViewModel.GeneratedRecipe;
        Messages.Clear();
        ErrorMessage = null;
        return Task.CompletedTask;
    }

    // Loads a specific saved recipe. Sets the flag to prevent InitAsync from overwriting it when the page navigates.

    public void LoadRecipe(RecipeOutputModel recipe)
    {
        _loadedFromSaved = true;
        CurrentRecipe = recipe;
        Messages.Clear();
        ErrorMessage = null;
    }

    /// <summary>
    /// Sends a user message to the AI, updates the conversation history,
    /// and applies the returned updated recipe.
    /// </summary>
    public async Task SendMessageAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage) || CurrentRecipe is null)
            return;

        ErrorMessage = null;
        IsSending = true;

        // Add user message to history
        Messages.Add(new ChatMessageModel { Role = "user", Content = userMessage.Trim() });
        var userEntry = new ChatMessageModel { Role = "user", Content = userMessage.Trim() };
        Messages.Add(userEntry);

        var (success, error, response) = await _apiService.SendChatMessageAsync(
            Messages,
            CurrentRecipe,
            userMessage.Trim()
        );

        IsSending = false;

        if (!success || response is null)
        {
            ErrorMessage = error;
            Messages.RemoveAt(Messages.Count - 1);
            return;
        }

        // Add assistant reply to history
        Messages.Add(new ChatMessageModel { Role = "assistant", Content = response.Reply });
        CurrentRecipe = response.Recipe;
    }

    /// <summary>
    /// Saves the current recipe to local storage under the given name.
    /// Always builds the summary card from CurrentRecipe so cook time,
    /// servings, and all other fields reflect any chat edits.
    /// </summary>
    public async Task SaveRecipeAsync(string saveName, string? imageBase64 = null, string? imageMimeType = null)
    {
        if (CurrentRecipe is null)
            return;

        var card = new RecipeCardModel
        {
            RecipeName = CurrentRecipe.RecipeName,
            Description = CurrentRecipe.Description,
            CuisineType = CurrentRecipe.CuisineType,
            Difficulty = CurrentRecipe.Difficulty,
            MealType = CurrentRecipe.MealType,
            CookTimeMinutes = CurrentRecipe.CookTimeMinutes,
            PrepTimeMinutes = CurrentRecipe.PrepTimeMinutes,
            Servings = CurrentRecipe.Servings,
            Ingredients = CurrentRecipe.Ingredients.Select(i => i.Name).ToList()
        };

        var saved = new SavedRecipeModel
        {
            SaveName = saveName,
            Recipe = CurrentRecipe,
            Card = card,
            SavedAt = DateTime.Now,
            ImageBase64 = imageBase64,
            ImageMimeType = imageMimeType
        };

        await _storage.SaveRecipeAsync(saved);

        // Clear the saved flag so generating a new recipe later works correctly
        _loadedFromSaved = false;
    }
}