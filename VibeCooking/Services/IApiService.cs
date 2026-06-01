using VibeCooking.Models;

namespace VibeCooking.Services;

public interface IApiService
{
    // Generates 1-5 recipe cards for the user to choose from. Called first when users generate recipes
    Task<(bool success, string errorMessage, List<RecipeCardModel> cards)> GenerateRecipeCardsAsync(RecipeParameterModel parameters);

    // Generates the full recipe based on a chosen card.
    Task<(bool success, string errorMessage, RecipeOutputModel? output)> GenerateRecipeAsync(RecipeCardModel chosenCard, RecipeParameterModel parameters);

    // Sends a chat message to the AI
    Task<(bool success, string errorMessage, ChatResponseModel? response)> SendChatMessageAsync(
        List<ChatMessageModel> history,
        RecipeOutputModel currentRecipe,
        string userMessage);
}