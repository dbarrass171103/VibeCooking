using Moq;
using VibeCooking.Models;
using VibeCooking.Services;
using VibeCooking.ViewModels;

namespace VibeCooking.Tests.Tests;

public class MealPrepViewModelTests
{
    private static (MealPrepViewModel vm, Mock<ILocalStorageService> mockStorage) BuildVm(
        Dictionary<DateTime, List<string>>? calendarData = null,
        List<SavedRecipeModel>? savedRecipes = null)
    {
        var mockStorage = new Mock<ILocalStorageService>();

        mockStorage
            .Setup(x => x.LoadRecipesAsync())
            .ReturnsAsync(savedRecipes ?? new List<SavedRecipeModel>());

        mockStorage
            .Setup(x => x.LoadCalendarRecipesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(calendarData ?? new Dictionary<DateTime, List<string>>());

        var vm = new MealPrepViewModel(mockStorage.Object);
        return (vm, mockStorage);
    }

    private static SavedRecipeModel MakeRecipe(string id, string name, params string[] ingredients)
    {
        var recipe = new SavedRecipeModel
        {
            Id = Guid.Parse(id),
            SaveName = name,
            Recipe = new RecipeOutputModel
            {
                RecipeName = name,
                Ingredients = ingredients
                    .Select(i => new IngredientModel { Name = i, Quantity = "100g" })
                    .ToList()
            }
        };
        return recipe;
    }

    // ──────────────────────────────────────────
    // Week navigation
    // ──────────────────────────────────────────

    [Fact]
    public async Task InitAsync_CurrentWeekStart_IsMonday()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();

        // Monday = DayOfWeek 1
        Assert.Equal(DayOfWeek.Monday, vm.CurrentWeekStart.DayOfWeek);
    }

    [Fact]
    public async Task CurrentWeekEnd_Is7DaysAfterStart()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();

        Assert.Equal(vm.CurrentWeekStart.AddDays(7), vm.CurrentWeekEnd);
    }

    [Fact]
    public async Task GoToNextWeekAsync_AdvancesWeekBySevenDays()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();
        var original = vm.CurrentWeekStart;

        await vm.GoToNextWeekAsync();

        Assert.Equal(original.AddDays(7), vm.CurrentWeekStart);
    }

    [Fact]
    public async Task GoToPreviousWeekAsync_MovesWeekBackSevenDays()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();
        var original = vm.CurrentWeekStart;

        await vm.GoToPreviousWeekAsync();

        Assert.Equal(original.AddDays(-7), vm.CurrentWeekStart);
    }

    [Fact]
    public async Task GoToNextThenPrevious_ReturnsToOriginalWeek()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();
        var original = vm.CurrentWeekStart;

        await vm.GoToNextWeekAsync();
        await vm.GoToPreviousWeekAsync();

        Assert.Equal(original, vm.CurrentWeekStart);
    }

    // ──────────────────────────────────────────
    // WeekDays structure
    // ──────────────────────────────────────────

    [Fact]
    public async Task InitAsync_WeekDays_AlwaysHasSevenEntries()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();

        Assert.Equal(7, vm.WeekDays.Count);
    }

    [Fact]
    public async Task InitAsync_WeekDays_StartsOnMonday()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();

        Assert.Equal(DayOfWeek.Monday, vm.WeekDays[0].Date.DayOfWeek);
    }

    [Fact]
    public async Task InitAsync_WeekDays_EndsOnSunday()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();

        Assert.Equal(DayOfWeek.Sunday, vm.WeekDays[6].Date.DayOfWeek);
    }

    [Fact]
    public async Task InitAsync_WeekDays_DatesAreConsecutive()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();

        for (int i = 1; i < vm.WeekDays.Count; i++)
        {
            Assert.Equal(
                vm.WeekDays[i - 1].Date.AddDays(1),
                vm.WeekDays[i].Date
            );
        }
    }

    // ──────────────────────────────────────────
    // Meal loading
    // ──────────────────────────────────────────

    [Fact]
    public async Task InitAsync_NoCalendarData_AllDaysHaveEmptyMeals()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();

        Assert.All(vm.WeekDays, day => Assert.Empty(day.Meals));
    }

    [Fact]
    public async Task InitAsync_WithCalendarData_PopulatesMealNamesOnCorrectDay()
    {
        const string recipeId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        var recipe = MakeRecipe(recipeId, "Chicken Pasta");

        // Find the Monday of the current week to pin the test date
        var today = DateTime.Today;
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysFromMonday);

        var calendarData = new Dictionary<DateTime, List<string>>
        {
            { monday, new List<string> { recipeId } }
        };

        var (vm, _) = BuildVm(calendarData, savedRecipes: [recipe]);
        await vm.InitAsync();

        var mondayEntry = vm.WeekDays.First(d => d.Date.DayOfWeek == DayOfWeek.Monday);
        Assert.Single(mondayEntry.Meals);
        Assert.Equal("Chicken Pasta", mondayEntry.Meals[0].Name);
        Assert.Equal(recipeId, mondayEntry.Meals[0].RecipeId);
    }

    [Fact]
    public async Task InitAsync_UnknownRecipeId_IsSkippedGracefully()
    {
        var today = DateTime.Today;
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysFromMonday);

        var calendarData = new Dictionary<DateTime, List<string>>
        {
            { monday, new List<string> { "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" } }
        };

        // No matching saved recipe provided
        var (vm, _) = BuildVm(calendarData, savedRecipes: []);
        await vm.InitAsync();

        Assert.All(vm.WeekDays, day => Assert.Empty(day.Meals));
    }

    // ──────────────────────────────────────────
    // WeekIngredients
    // ──────────────────────────────────────────

    [Fact]
    public async Task InitAsync_WithRecipe_PopulatesWeekIngredients()
    {
        const string recipeId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
        var recipe = MakeRecipe(recipeId, "Test Recipe", "Chicken", "Rice", "Garlic");

        var today = DateTime.Today;
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysFromMonday);

        var calendarData = new Dictionary<DateTime, List<string>>
        {
            { monday, new List<string> { recipeId } }
        };

        var (vm, _) = BuildVm(calendarData, savedRecipes: [recipe]);
        await vm.InitAsync();

        var ingredientNames = vm.WeekIngredients.Select(i => i.Name).ToList();
        Assert.Contains("Chicken", ingredientNames);
        Assert.Contains("Rice", ingredientNames);
        Assert.Contains("Garlic", ingredientNames);
    }

    [Fact]
    public async Task InitAsync_WeekIngredients_IncludesQuantity()
    {
        const string recipeId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
        var recipe = new SavedRecipeModel
        {
            Id = Guid.Parse(recipeId),
            SaveName = "Test",
            Recipe = new RecipeOutputModel
            {
                Ingredients = [new IngredientModel { Name = "Salmon", Quantity = "200g" }]
            }
        };

        var today = DateTime.Today;
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysFromMonday);

        var calendarData = new Dictionary<DateTime, List<string>>
        {
            { monday, new List<string> { recipeId } }
        };

        var (vm, _) = BuildVm(calendarData, savedRecipes: [recipe]);
        await vm.InitAsync();

        var ingredient = vm.WeekIngredients.First(i => i.Name == "Salmon");
        Assert.Equal("200g", ingredient.Quantity);
    }

    [Fact]
    public async Task InitAsync_NoRecipes_WeekIngredientsIsEmpty()
    {
        var (vm, _) = BuildVm();
        await vm.InitAsync();

        Assert.Empty(vm.WeekIngredients);
    }

    // ──────────────────────────────────────────
    // MapDateToRecipeAsync / RemoveMealAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task MapDateToRecipeAsync_CallsSaveCalendarRecipe()
    {
        var (vm, mockStorage) = BuildVm();
        await vm.InitAsync();

        var date = DateTime.Today;
        await vm.MapDateToRecipeAsync(date, "some-recipe-id");

        mockStorage.Verify(
            x => x.SaveCalendarRecipeAsync(
                It.Is<KeyValuePair<DateTime, string>>(kv =>
                    kv.Key == date && kv.Value == "some-recipe-id")),
            Times.Once);
    }

    [Fact]
    public async Task RemoveMealAsync_CallsRemoveCalendarRecipe()
    {
        var (vm, mockStorage) = BuildVm();
        await vm.InitAsync();

        var date = DateTime.Today;
        await vm.RemoveMealAsync(date, "some-recipe-id");

        mockStorage.Verify(
            x => x.RemoveCalendarRecipeAsync(date, "some-recipe-id"),
            Times.Once);
    }

    [Fact]
    public async Task MapDateToRecipeAsync_ReloadsWeekAfterSaving()
    {
        var loadCallCount = 0;
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage.Setup(x => x.LoadRecipesAsync()).ReturnsAsync(new List<SavedRecipeModel>());
        mockStorage
            .Setup(x => x.LoadCalendarRecipesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(() =>
            {
                loadCallCount++;
                return new Dictionary<DateTime, List<string>>();
            });

        var vm = new MealPrepViewModel(mockStorage.Object);
        await vm.InitAsync(); // First load

        await vm.MapDateToRecipeAsync(DateTime.Today, "id");

        // Should have loaded at least twice: once on Init, once after Map
        Assert.True(loadCallCount >= 2);
    }
}
