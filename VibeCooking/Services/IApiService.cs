using VibeCooking.Models;

namespace VibeCooking.Services;

public interface IApiService
{
    // Generates ~5 recipe cards for the user to choose from. Called first when users generate recipes
    public Task<(bool success, string errorMessage, List<RecipeCardModel> cards)> GenerateRecipeCardsAsync(RecipeParameterModel parameters);

    // Generates the full recipe based on a chosen card. Called when user selects recipe they like
    public Task<(bool success, string errorMessage, RecipeOutputModel? output)> GenerateRecipeAsync(RecipeCardModel chosenCard, RecipeParameterModel parameters);
}