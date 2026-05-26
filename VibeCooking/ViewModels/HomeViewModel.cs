using VibeCooking.Models;

namespace VibeCooking.ViewModels;

public class HomeViewModel : BaseViewModel
{
	public List<SavedRecipeModel> SavedRecipes { get; set; } = new();

	public override async Task InitAsync()
	{
		try
		{
			await LoadSavedRecipesAsync(numRecipes: 3);
		}
		catch (Exception ex)
		{
			throw;
		}
	}

	private async Task LoadSavedRecipesAsync(int numRecipes)
	{
		//TODO - load the last 3 saved recipes from the local storage to show on homepage
		throw new NotImplementedException("Failed to load saved recipes");
	}
}