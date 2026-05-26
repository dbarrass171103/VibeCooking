using VibeCooking.Models;

namespace VibeCooking.Services;

public class ApiService : IApiService
{
	public async Task<(bool success, string errorMessage, RecipeOutputModel? output)> GenerateRecipeAsync(RecipeParameterModel parameters)
	{
		RecipeOutputModel output = new();

		try
		{
			//TODO: Implement this. Needs to serialise into output and return it.
			throw new NotImplementedException();
		}
		catch (Exception ex)
		{
			return (false, ex.Message, null);
		}

		return (true, "", output);
	}
}