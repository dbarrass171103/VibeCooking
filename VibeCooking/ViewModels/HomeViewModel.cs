using VibeCooking.Models;

namespace VibeCooking.ViewModels;

public class HomeViewModel : BaseViewModel
{
	public List<SavedRecipeModel> SavedRecipes { get; set; } = new();

	public override async Task InitAsync()
	{
		//TODO Load the last 3 saved recipes from the local database/storage? and load into the list. THese will be automatically displayed on the homepage.
	}
}