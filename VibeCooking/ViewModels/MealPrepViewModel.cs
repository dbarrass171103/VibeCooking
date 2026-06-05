using Heron.MudCalendar;
using Heron.MudTotalCalendar;
using VibeCooking.Models;
using VibeCooking.Services;

namespace VibeCooking.ViewModels;

public class MealPrepViewModel : BaseViewModel
{
	private readonly ILocalStorageService _localStorageService;

	public List<CalendarItem> CalendarItems { get; set; } = new();
	public List<Value> CalendarTotalItems { get; set; } = new();

	public List<SavedRecipeModel> SavedRecipes { get; set; } = new();
	public Dictionary<DateTime, List<string>> LoadedCalendarRecipes { get; set; } = new();

	public MealPrepViewModel(ILocalStorageService localStorageService)
	{
		_localStorageService = localStorageService;
	}

	public override async Task InitAsync()
	{
		SavedRecipes = await _localStorageService.LoadRecipesAsync();
		var today = DateTime.Today;

		int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;

		var startOfWeek = today.AddDays(-daysFromMonday);
		var endOfWeek = startOfWeek.AddDays(7);

		await ReloadCalendarRecipesAsync(startOfWeek, endOfWeek);
	}

	public async Task ReloadCalendarRecipesAsync(DateTime start, DateTime end)
	{
		LoadedCalendarRecipes = await _localStorageService.LoadCalendarRecipesAsync(start, end);
		CalendarItems = new();

		Dictionary<DateTime, List<string>> ingredientsTotals = new();

		foreach (var keyValue in LoadedCalendarRecipes)
		{
			foreach (string recipeUUID in keyValue.Value)
			{
				SavedRecipeModel? savedRecipe = SavedRecipes.Where(x => x.Id.ToString() == recipeUUID).FirstOrDefault() ?? null;

				if (savedRecipe != null)
				{
					List<string> savedRecipeIngredients = savedRecipe.Recipe.Ingredients.Select(x => x.Name).ToList();
					ingredientsTotals.Add(keyValue.Key, savedRecipeIngredients);

					CalendarItems.Add(new CalendarItem()
					{
						Text = savedRecipe.SaveName,
						Start = keyValue.Key
					});
				}
			}
		}

		await ReloadTotalIngredientsAsync(ingredientsTotals);
	}

	private async Task ReloadTotalIngredientsAsync(Dictionary<DateTime, List<string>> ingredientsTotals)
	{
		CalendarTotalItems = new();

		foreach (var keyValue in ingredientsTotals)
		{
			foreach (string ingredient in keyValue.Value)
			{
				CalendarTotalItems.Add(new()
				{
					Date = keyValue.Key,
					Definition = new ValueDefinition()
					{
						Name = ingredient
					}
				});
			}
		}
	}

	public async Task MapDateToRecipeAsync(DateTime date, string recipeId)
	{
		await _localStorageService.SaveCalendarRecipeAsync(new KeyValuePair<DateTime, string>(date, recipeId));
	}
}