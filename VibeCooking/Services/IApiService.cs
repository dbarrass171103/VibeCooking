using VibeCooking.Models;

namespace VibeCooking.Services;

public interface IApiService
{
	public Task<RecipeOutputModel> GenerateRecipeAsync(RecipeParameterModel parameters);
}