using VibeCooking.Models;
using VibeCooking.Services;

namespace VibeCooking.ViewModels;

public class RecipeChatViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly RecipeGenerationViewModel _recipeGenerationViewModel;

    // The live recipe — starts as the generated recipe and updates with each chat turn
    public RecipeOutputModel? CurrentRecipe { get; private set; }
    // Full conversation history
    public List<ChatMessageModel> Messages { get; private set; } = new();

    public bool IsSending { get; private set; } = false;
    public string? ErrorMessage { get; private set; }

    // True once the recipe has been loaded
    public bool IsReady => CurrentRecipe is not null;

    public RecipeChatViewModel(IApiService apiService, RecipeGenerationViewModel recipeGenerationViewModel)
    {
        _apiService = apiService;
        _recipeGenerationViewModel = recipeGenerationViewModel;
    }

    // Loads the generated recipe from RecipeGenerationViewModel.
    public override Task InitAsync()
    {
        CurrentRecipe = _recipeGenerationViewModel.GeneratedRecipe;
        Messages.Clear();
        ErrorMessage = null;
        return Task.CompletedTask;
    }
   
    // Loads a specific recipe directly
    public void LoadRecipe(RecipeOutputModel recipe)
    {
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

        // Update the live recipe
        CurrentRecipe = response.Recipe;
    }
}