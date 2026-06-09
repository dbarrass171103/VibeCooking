using Moq;
using VibeCooking.Models;
using VibeCooking.Services;
using VibeCooking.ViewModels;

namespace VibeCooking.Tests.Tests;

public class LocalStorageServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private const string RecipesKey = "saved_recipes";
    private const string CalendarKey = "calendar_recipes";

    /// Builds a service backed by a fresh in-memory barrel mock.
    private static (LocalStorageService svc, Mock<IBarrel> mockBarrel) Build()
    {
        var mockBarrel = new Mock<IBarrel>();

        // Default: every key is expired (nothing stored yet)
        mockBarrel.Setup(x => x.IsExpired(It.IsAny<string>())).Returns(true);

        var svc = new LocalStorageService(mockBarrel.Object);
        return (svc, mockBarrel);
    }

    /// Seeds the mock barrel so IsExpired returns false and Get returns the given value.
    private static void Seed<T>(Mock<IBarrel> barrel, string key, T value)
    {
        barrel.Setup(x => x.IsExpired(key)).Returns(false);
        barrel.Setup(x => x.Get<T>(key)).Returns(value);
    }

    private static SavedRecipeModel MakeRecipe(string name, DateTime? savedAt = null) => new()
    {
        SaveName = name,
        SavedAt = savedAt ?? DateTime.Now,
        Recipe = new RecipeOutputModel { RecipeName = name }
    };

    // ── SaveRecipeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SaveRecipeAsync_AddsNewRecipe_WhenNotAlreadyStored()
    {
        var (svc, barrel) = Build();
        var recipe = MakeRecipe("New Recipe");

        await svc.SaveRecipeAsync(recipe);

        barrel.Verify(x => x.Add(
            RecipesKey,
            It.Is<List<SavedRecipeModel>>(list => list.Any(r => r.Id == recipe.Id)),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveRecipeAsync_UpdatesExistingRecipe_WhenIdMatches()
    {
        var (svc, barrel) = Build();
        var existing = MakeRecipe("Old Name");
        Seed(barrel, RecipesKey, new List<SavedRecipeModel> { existing });

        // Same ID, updated name
        existing.SaveName = "Updated Name";
        await svc.SaveRecipeAsync(existing);

        barrel.Verify(x => x.Add(
            RecipesKey,
            It.Is<List<SavedRecipeModel>>(list =>
                list.Count == 1 &&
                list[0].SaveName == "Updated Name"),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveRecipeAsync_DoesNotDuplicate_ExistingRecipe()
    {
        var (svc, barrel) = Build();
        var recipe = MakeRecipe("Recipe");
        Seed(barrel, RecipesKey, new List<SavedRecipeModel> { recipe });

        await svc.SaveRecipeAsync(recipe);

        barrel.Verify(x => x.Add(
            RecipesKey,
            It.Is<List<SavedRecipeModel>>(list => list.Count == 1),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    // ── LoadRecipesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task LoadRecipesAsync_ReturnsEmpty_WhenNothingStored()
    {
        var (svc, _) = Build(); // all keys expired by default

        var result = await svc.LoadRecipesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadRecipesAsync_ReturnsStoredRecipes()
    {
        var (svc, barrel) = Build();
        var recipes = new List<SavedRecipeModel>
        {
            MakeRecipe("Pasta"),
            MakeRecipe("Soup")
        };
        Seed(barrel, RecipesKey, recipes);

        var result = await svc.LoadRecipesAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task LoadRecipesAsync_OrdersByMostRecentFirst()
    {
        var (svc, barrel) = Build();
        var older = MakeRecipe("Old Recipe", savedAt: DateTime.Now.AddDays(-2));
        var newer = MakeRecipe("New Recipe", savedAt: DateTime.Now);
        Seed(barrel, RecipesKey, new List<SavedRecipeModel> { older, newer });

        var result = await svc.LoadRecipesAsync();

        Assert.Equal("New Recipe", result[0].SaveName);
        Assert.Equal("Old Recipe", result[1].SaveName);
    }

    // ── DeleteRecipeAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRecipeAsync_RemovesRecipeById()
    {
        var (svc, barrel) = Build();
        var recipe = MakeRecipe("To Delete");
        Seed(barrel, RecipesKey, new List<SavedRecipeModel> { recipe });

        await svc.DeleteRecipeAsync(recipe.Id);

        barrel.Verify(x => x.Add(
            RecipesKey,
            It.Is<List<SavedRecipeModel>>(list => list.All(r => r.Id != recipe.Id)),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRecipeAsync_LeavesOtherRecipesIntact()
    {
        var (svc, barrel) = Build();
        var keep = MakeRecipe("Keep");
        var delete = MakeRecipe("Delete");
        Seed(barrel, RecipesKey, new List<SavedRecipeModel> { keep, delete });

        await svc.DeleteRecipeAsync(delete.Id);

        barrel.Verify(x => x.Add(
            RecipesKey,
            It.Is<List<SavedRecipeModel>>(list =>
                list.Count == 1 && list[0].Id == keep.Id),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRecipeAsync_UnknownId_WritesUnchangedList()
    {
        var (svc, barrel) = Build();
        var recipe = MakeRecipe("Existing");
        Seed(barrel, RecipesKey, new List<SavedRecipeModel> { recipe });

        await svc.DeleteRecipeAsync(Guid.NewGuid());

        barrel.Verify(x => x.Add(
            RecipesKey,
            It.Is<List<SavedRecipeModel>>(list => list.Count == 1),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    // ── SaveCalendarRecipeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SaveCalendarRecipeAsync_CreatesNewDateEntry_WhenDateNotPresent()
    {
        var (svc, barrel) = Build();
        var date = new DateTime(2025, 1, 6);

        await svc.SaveCalendarRecipeAsync(new KeyValuePair<DateTime, string>(date, "recipe-id-1"));

        barrel.Verify(x => x.Add(
            CalendarKey,
            It.Is<Dictionary<DateTime, List<string>>>(d =>
                d.ContainsKey(date) && d[date].Contains("recipe-id-1")),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveCalendarRecipeAsync_AppendsToExistingDate_WhenDateAlreadyPresent()
    {
        var (svc, barrel) = Build();
        var date = new DateTime(2025, 1, 6);
        Seed(barrel, CalendarKey, new Dictionary<DateTime, List<string>>
        {
            { date, ["existing-id"] }
        });

        await svc.SaveCalendarRecipeAsync(new KeyValuePair<DateTime, string>(date, "new-id"));

        barrel.Verify(x => x.Add(
            CalendarKey,
            It.Is<Dictionary<DateTime, List<string>>>(d =>
                d[date].Contains("existing-id") && d[date].Contains("new-id")),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    // ── RemoveCalendarRecipeAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RemoveCalendarRecipeAsync_RemovesSpecificRecipeFromDate()
    {
        var (svc, barrel) = Build();
        var date = new DateTime(2025, 1, 6);
        Seed(barrel, CalendarKey, new Dictionary<DateTime, List<string>>
        {
            { date, ["id-1", "id-2"] }
        });

        await svc.RemoveCalendarRecipeAsync(date, "id-1");

        barrel.Verify(x => x.Add(
            CalendarKey,
            It.Is<Dictionary<DateTime, List<string>>>(d =>
                d[date].Count == 1 && d[date].Contains("id-2")),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveCalendarRecipeAsync_RemovesDateKey_WhenLastRecipeRemoved()
    {
        var (svc, barrel) = Build();
        var date = new DateTime(2025, 1, 6);
        Seed(barrel, CalendarKey, new Dictionary<DateTime, List<string>>
        {
            { date, ["only-id"] }
        });

        await svc.RemoveCalendarRecipeAsync(date, "only-id");

        barrel.Verify(x => x.Add(
            CalendarKey,
            It.Is<Dictionary<DateTime, List<string>>>(d => !d.ContainsKey(date)),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveCalendarRecipeAsync_UnknownDate_StillWritesSuccessfully()
    {
        var (svc, barrel) = Build(); // empty barrel

        var ex = await Record.ExceptionAsync(() =>
            svc.RemoveCalendarRecipeAsync(new DateTime(2025, 1, 6), "any-id"));

        Assert.Null(ex);
    }

    // ── LoadCalendarRecipesAsync (date range) ─────────────────────────────────

    [Fact]
    public async Task LoadCalendarRecipesAsync_ReturnsEmpty_WhenNothingStored()
    {
        var (svc, _) = Build();

        var result = await svc.LoadCalendarRecipesAsync(DateTime.Today, DateTime.Today.AddDays(7));

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadCalendarRecipesAsync_ReturnsOnlyDatesWithinRange()
    {
        var (svc, barrel) = Build();
        var inRange = new DateTime(2025, 1, 8);
        var outOfRange = new DateTime(2025, 1, 20);

        Seed(barrel, CalendarKey, new Dictionary<DateTime, List<string>>
        {
            { inRange,    ["id-1"] },
            { outOfRange, ["id-2"] }
        });

        var result = await svc.LoadCalendarRecipesAsync(
            new DateTime(2025, 1, 6),
            new DateTime(2025, 1, 12));

        Assert.True(result.ContainsKey(inRange));
        Assert.False(result.ContainsKey(outOfRange));
    }

    [Fact]
    public async Task LoadCalendarRecipesAsync_IncludesDatesOnRangeBoundaries()
    {
        var (svc, barrel) = Build();
        var start = new DateTime(2025, 1, 6);
        var end = new DateTime(2025, 1, 12);

        Seed(barrel, CalendarKey, new Dictionary<DateTime, List<string>>
        {
            { start, ["start-id"] },
            { end,   ["end-id"] }
        });

        var result = await svc.LoadCalendarRecipesAsync(start, end);

        Assert.True(result.ContainsKey(start));
        Assert.True(result.ContainsKey(end));
    }

    // ── SaveIngredientsAsync ──────────────────────────────────────────────────

    private const string IngredientsKey = "ingredients_selections";
    private const string CustomIngredientsKey = "ingredients_custom";

    /// Creates a real IngredientsViewModel with all category keys populated, then seeds
    /// it with the provided ingredients so SaveIngredientsAsync has something to snapshot.
    private static IngredientsViewModel MakeViewModel(params UserIngredient[] ingredients)
    {
        var mockLoader = new Mock<IIngredientCatalogLoader>();
        mockLoader.Setup(x => x.LoadCatalogJsonAsync()).ReturnsAsync("[]");

        var vm = new IngredientsViewModel(
            new Mock<ILocalStorageService>().Object,
            mockLoader.Object);
        vm.InitAsync().GetAwaiter().GetResult();

        foreach (var ingredient in ingredients)
            vm.IngredientsByCategory[ingredient.Category].Add(ingredient);

        return vm;
    }

    [Fact]
    public async Task SaveIngredientsAsync_SavesCatalogIngredients_ToIngredientsKey()
    {
        var (svc, barrel) = Build();
        var vm = MakeViewModel(
            new UserIngredient { Name = "Chicken", Category = IngredientCategory.Meat, IsCustom = false });

        await svc.SaveIngredientsAsync(vm);

        barrel.Verify(x => x.Add(
            IngredientsKey,
            It.IsAny<List<IngredientSnapshot>>(),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveIngredientsAsync_SavesCustomIngredients_ToCustomKey()
    {
        var (svc, barrel) = Build();
        var vm = MakeViewModel(
            new UserIngredient { Name = "Tofu", Category = IngredientCategory.Meat, IsCustom = true });

        await svc.SaveIngredientsAsync(vm);

        barrel.Verify(x => x.Add(
            CustomIngredientsKey,
            It.IsAny<List<IngredientSnapshot>>(),
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveIngredientsAsync_CatalogSnapshot_CapturesIsSelected()
    {
        var (svc, barrel) = Build();
        List<IngredientSnapshot>? captured = null;
        barrel
            .Setup(x => x.Add(IngredientsKey, It.IsAny<List<IngredientSnapshot>>(), It.IsAny<TimeSpan>()))
            .Callback<string, List<IngredientSnapshot>, TimeSpan>((_, v, _) => captured = v);

        var vm = MakeViewModel(
            new UserIngredient { Name = "Chicken", Category = IngredientCategory.Meat, IsSelected = true, IsCustom = false });

        await svc.SaveIngredientsAsync(vm);

        Assert.NotNull(captured);
        Assert.True(captured!.Single(s => s.Name == "Chicken").IsSelected);
    }

    [Fact]
    public async Task SaveIngredientsAsync_CatalogSnapshot_CapturesQuantity()
    {
        var (svc, barrel) = Build();
        List<IngredientSnapshot>? captured = null;
        barrel
            .Setup(x => x.Add(IngredientsKey, It.IsAny<List<IngredientSnapshot>>(), It.IsAny<TimeSpan>()))
            .Callback<string, List<IngredientSnapshot>, TimeSpan>((_, v, _) => captured = v);

        var vm = MakeViewModel(
            new UserIngredient { Name = "Rice", Category = IngredientCategory.Grains, Quantity = "200g", IsCustom = false });

        await svc.SaveIngredientsAsync(vm);

        Assert.Equal("200g", captured!.Single(s => s.Name == "Rice").Quantity);
    }

    [Fact]
    public async Task SaveIngredientsAsync_SeparatesCatalogFromCustom()
    {
        var (svc, barrel) = Build();
        List<IngredientSnapshot>? capturedCatalog = null;
        List<IngredientSnapshot>? capturedCustom = null;

        barrel
            .Setup(x => x.Add(IngredientsKey, It.IsAny<List<IngredientSnapshot>>(), It.IsAny<TimeSpan>()))
            .Callback<string, List<IngredientSnapshot>, TimeSpan>((_, v, _) => capturedCatalog = v);
        barrel
            .Setup(x => x.Add(CustomIngredientsKey, It.IsAny<List<IngredientSnapshot>>(), It.IsAny<TimeSpan>()))
            .Callback<string, List<IngredientSnapshot>, TimeSpan>((_, v, _) => capturedCustom = v);

        var vm = MakeViewModel(
            new UserIngredient { Name = "Chicken", Category = IngredientCategory.Meat, IsCustom = false },
            new UserIngredient { Name = "Tofu", Category = IngredientCategory.Meat, IsCustom = true });

        await svc.SaveIngredientsAsync(vm);

        Assert.Contains(capturedCatalog!, s => s.Name == "Chicken");
        Assert.DoesNotContain(capturedCatalog!, s => s.Name == "Tofu");
        Assert.Contains(capturedCustom!, s => s.Name == "Tofu");
        Assert.DoesNotContain(capturedCustom!, s => s.Name == "Chicken");
    }

    [Fact]
    public async Task SaveIngredientsAsync_EmptyViewModel_SavesEmptyLists()
    {
        var (svc, barrel) = Build();
        List<IngredientSnapshot>? capturedCatalog = null;
        List<IngredientSnapshot>? capturedCustom = null;

        barrel
            .Setup(x => x.Add(IngredientsKey, It.IsAny<List<IngredientSnapshot>>(), It.IsAny<TimeSpan>()))
            .Callback<string, List<IngredientSnapshot>, TimeSpan>((_, v, _) => capturedCatalog = v);
        barrel
            .Setup(x => x.Add(CustomIngredientsKey, It.IsAny<List<IngredientSnapshot>>(), It.IsAny<TimeSpan>()))
            .Callback<string, List<IngredientSnapshot>, TimeSpan>((_, v, _) => capturedCustom = v);

        var vm = MakeViewModel(); // no ingredients
        await svc.SaveIngredientsAsync(vm);

        Assert.Empty(capturedCatalog!);
        Assert.Empty(capturedCustom!);
    }

    // ── LoadIngredientsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task LoadIngredientsAsync_WhenKeyExpired_DoesNotRestoreState()
    {
        var (svc, _) = Build(); // all keys expired by default
        var vm = MakeViewModel(
            new UserIngredient { Name = "Chicken", Category = IngredientCategory.Meat, IsSelected = false });

        await svc.LoadIngredientsAsync(vm);

        var chicken = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Chicken");
        Assert.False(chicken.IsSelected);
    }

    [Fact]
    public async Task LoadIngredientsAsync_RestoresIsSelected_FromSnapshot()
    {
        var (svc, barrel) = Build();
        Seed(barrel, IngredientsKey, new List<IngredientSnapshot>
        {
            new("Chicken", IngredientCategory.Meat, IsSelected: true, Quantity: "")
        });

        var vm = MakeViewModel(
            new UserIngredient { Name = "Chicken", Category = IngredientCategory.Meat, IsSelected = false });

        await svc.LoadIngredientsAsync(vm);

        var chicken = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Chicken");
        Assert.True(chicken.IsSelected);
    }

    [Fact]
    public async Task LoadIngredientsAsync_RestoresQuantity_FromSnapshot()
    {
        var (svc, barrel) = Build();
        Seed(barrel, IngredientsKey, new List<IngredientSnapshot>
        {
            new("Rice", IngredientCategory.Grains, IsSelected: true, Quantity: "300g")
        });

        var vm = MakeViewModel(
            new UserIngredient { Name = "Rice", Category = IngredientCategory.Grains });

        await svc.LoadIngredientsAsync(vm);

        var rice = vm.IngredientsByCategory[IngredientCategory.Grains].First(i => i.Name == "Rice");
        Assert.Equal("300g", rice.Quantity);
    }

    [Fact]
    public async Task LoadIngredientsAsync_NameMatching_IsCaseInsensitive()
    {
        var (svc, barrel) = Build();
        Seed(barrel, IngredientsKey, new List<IngredientSnapshot>
        {
            new("CHICKEN", IngredientCategory.Meat, IsSelected: true, Quantity: "")
        });

        var vm = MakeViewModel(
            new UserIngredient { Name = "Chicken", Category = IngredientCategory.Meat, IsSelected = false });

        await svc.LoadIngredientsAsync(vm);

        var chicken = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Chicken");
        Assert.True(chicken.IsSelected);
    }

    [Fact]
    public async Task LoadIngredientsAsync_IngredientNotInSnapshot_RemainsUnchanged()
    {
        var (svc, barrel) = Build();
        // Snapshot only has Chicken — Beef is not in it
        Seed(barrel, IngredientsKey, new List<IngredientSnapshot>
        {
            new("Chicken", IngredientCategory.Meat, IsSelected: true, Quantity: "")
        });

        var vm = MakeViewModel(
            new UserIngredient { Name = "Chicken", Category = IngredientCategory.Meat },
            new UserIngredient { Name = "Beef", Category = IngredientCategory.Meat, IsSelected = false });

        await svc.LoadIngredientsAsync(vm);

        var beef = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Beef");
        Assert.False(beef.IsSelected);
    }

    [Fact]
    public async Task LoadIngredientsAsync_WhenCustomKeyExpired_DoesNotAddCustomIngredients()
    {
        var (svc, _) = Build(); // all expired
        var vm = MakeViewModel();

        await svc.LoadIngredientsAsync(vm);

        Assert.Empty(vm.IngredientsByCategory[IngredientCategory.Meat]);
    }

    [Fact]
    public async Task LoadIngredientsAsync_AddsCustomIngredients_ToCorrectCategory()
    {
        var (svc, barrel) = Build();
        Seed(barrel, CustomIngredientsKey, new List<IngredientSnapshot>
        {
            new("Tofu", IngredientCategory.Meat, IsSelected: false, Quantity: "")
        });

        var vm = MakeViewModel();
        await svc.LoadIngredientsAsync(vm);

        Assert.Contains(vm.IngredientsByCategory[IngredientCategory.Meat], i => i.Name == "Tofu");
    }

    [Fact]
    public async Task LoadIngredientsAsync_CustomIngredient_HasIsCustomTrue()
    {
        var (svc, barrel) = Build();
        Seed(barrel, CustomIngredientsKey, new List<IngredientSnapshot>
        {
            new("Tofu", IngredientCategory.Meat, IsSelected: false, Quantity: "")
        });

        var vm = MakeViewModel();
        await svc.LoadIngredientsAsync(vm);

        var tofu = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        Assert.True(tofu.IsCustom);
    }

    [Fact]
    public async Task LoadIngredientsAsync_CustomIngredient_RestoresIsSelected()
    {
        var (svc, barrel) = Build();
        Seed(barrel, CustomIngredientsKey, new List<IngredientSnapshot>
        {
            new("Tofu", IngredientCategory.Meat, IsSelected: true, Quantity: "150g")
        });

        var vm = MakeViewModel();
        await svc.LoadIngredientsAsync(vm);

        var tofu = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        Assert.True(tofu.IsSelected);
        Assert.Equal("150g", tofu.Quantity);
    }

    [Fact]
    public async Task LoadIngredientsAsync_CustomIngredient_SetsBinaryFlag_ForSeasonings()
    {
        var (svc, barrel) = Build();
        Seed(barrel, CustomIngredientsKey, new List<IngredientSnapshot>
        {
            new("Custom Spice", IngredientCategory.Seasonings, IsSelected: false, Quantity: "")
        });

        var vm = MakeViewModel();
        await svc.LoadIngredientsAsync(vm);

        var spice = vm.IngredientsByCategory[IngredientCategory.Seasonings].First(i => i.Name == "Custom Spice");
        Assert.True(spice.IsBinary);
    }

    [Fact]
    public async Task LoadIngredientsAsync_CustomIngredient_DoesNotSetBinaryFlag_ForMeat()
    {
        var (svc, barrel) = Build();
        Seed(barrel, CustomIngredientsKey, new List<IngredientSnapshot>
        {
            new("Tofu", IngredientCategory.Meat, IsSelected: false, Quantity: "")
        });

        var vm = MakeViewModel();
        await svc.LoadIngredientsAsync(vm);

        var tofu = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        Assert.False(tofu.IsBinary);
    }

    [Fact]
    public async Task LoadIngredientsAsync_DoesNotAddDuplicateCustomIngredients()
    {
        var (svc, barrel) = Build();
        Seed(barrel, CustomIngredientsKey, new List<IngredientSnapshot>
        {
            new("Tofu", IngredientCategory.Meat, IsSelected: false, Quantity: "")
        });

        // Tofu is already in the catalog
        var vm = MakeViewModel(
            new UserIngredient { Name = "Tofu", Category = IngredientCategory.Meat });

        await svc.LoadIngredientsAsync(vm);

        Assert.Equal(1, vm.IngredientsByCategory[IngredientCategory.Meat]
            .Count(i => i.Name.Equals("Tofu", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task LoadIngredientsAsync_DuplicateCheck_IsCaseInsensitive()
    {
        var (svc, barrel) = Build();
        Seed(barrel, CustomIngredientsKey, new List<IngredientSnapshot>
        {
            new("TOFU", IngredientCategory.Meat, IsSelected: false, Quantity: "")
        });

        // Catalog has "Tofu" (different casing)
        var vm = MakeViewModel(
            new UserIngredient { Name = "Tofu", Category = IngredientCategory.Meat });

        await svc.LoadIngredientsAsync(vm);

        Assert.Equal(1, vm.IngredientsByCategory[IngredientCategory.Meat]
            .Count(i => i.Name.Equals("Tofu", StringComparison.OrdinalIgnoreCase)));
    }
}
