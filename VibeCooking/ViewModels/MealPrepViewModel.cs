using VibeCooking.Models;
using VibeCooking.Services;

namespace VibeCooking.ViewModels;

public class MealPrepViewModel : BaseViewModel
{
	private readonly ILocalStorageService _localStorageService;

	public DateTime CurrentWeekStart { get; private set; }
	public DateTime CurrentWeekEnd => CurrentWeekStart.AddDays(7);

	public List<DayMealEntry> WeekDays { get; set; } = new();
	public List<WeekIngredientEntry> WeekIngredients { get; set; } = new();

	public List<SavedRecipeModel> SavedRecipes { get; set; } = new();

	public MealPrepViewModel(ILocalStorageService localStorageService)
	{
		_localStorageService = localStorageService;
	}

	public override async Task InitAsync()
	{
		SavedRecipes = await _localStorageService.LoadRecipesAsync();

        var today = DateTime.Today;
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        CurrentWeekStart = today.AddDays(-daysFromMonday);

        await ReloadWeekAsync();
	}

	public async Task GoToPreviousWeekAsync()
	{
		CurrentWeekStart = CurrentWeekStart.AddDays(-7);
		await ReloadWeekAsync();
	}

	public async Task GoToNextWeekAsync()
	{
		CurrentWeekStart = CurrentWeekStart.AddDays(7);
		await ReloadWeekAsync();
	}

	private async Task ReloadWeekAsync()
	{
		var loaded = await _localStorageService.LoadCalendarRecipesAsync(CurrentWeekStart, CurrentWeekEnd);

		WeekDays = new();
		WeekIngredients = new();

		for (int i = 0; i < 7; i++)
		{
			var date = CurrentWeekStart.AddDays(i);
			var meals = new List<MealEntry>();

			if (loaded.TryGetValue(date, out var recipeIds))
			{
				foreach (var id in recipeIds)
				{
					var recipe = SavedRecipes.FirstOrDefault(x => x.Id.ToString() == id);
					if (recipe != null)
					{
						meals.Add(new MealEntry { RecipeId = id, Name = recipe.SaveName });
						WeekIngredients.AddRange(recipe.Recipe.Ingredients.Select(x => new WeekIngredientEntry { Name = x.Name, Quantity = x.Quantity }));
					}
				}
			}

			WeekDays.Add(new DayMealEntry { Date = date, Meals = meals });
		}
	}

	public async Task MapDateToRecipeAsync(DateTime date, string recipeId)
	{
		await _localStorageService.SaveCalendarRecipeAsync(new KeyValuePair<DateTime, string>(date, recipeId));
		await ReloadWeekAsync();
	}

	public async Task RemoveMealAsync(DateTime date, string recipeId)
	{
		await _localStorageService.RemoveCalendarRecipeAsync(date, recipeId);
		await ReloadWeekAsync();
	}
}

public class DayMealEntry
{
	public DateTime Date { get; set; }
	public List<MealEntry> Meals { get; set; } = new();
}

public class MealEntry
{
	public string RecipeId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
}

public class WeekIngredientEntry
{
	public string Name { get; set; } = string.Empty;
	public string Quantity { get; set; } = string.Empty;
}
