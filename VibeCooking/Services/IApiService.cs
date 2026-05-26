using VibeCooking.Models;

namespace VibeCooking.Services;

public interface IApiService
{
	public Task<(bool success, string errorMessage, RecipeOutputModel output)> GenerateRecipeAsync(RecipeParameterModel parameters);
}