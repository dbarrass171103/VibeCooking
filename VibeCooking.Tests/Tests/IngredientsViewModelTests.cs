using System.Text.Json;
using Moq;
using VibeCooking.Models;
using VibeCooking.Services;
using VibeCooking.ViewModels;

namespace VibeCooking.Tests.Tests;

public class IngredientsViewModelTests
{
    // ── Helpers ─────────────────────────────────────────────────────────────

    /// Serialises a minimal ingredient list into the JSON format LoadCatalogAsync expects.
    private static string CatalogJson(params (string name, IngredientCategory category)[] items)
    {
        var list = items.Select(i => new { name = i.name, category = i.category.ToString() });
        return JsonSerializer.Serialize(list);
    }

    private static (IngredientsViewModel vm,
                    Mock<ILocalStorageService> mockStorage,
                    Mock<IIngredientCatalogLoader> mockLoader)
        BuildVm(string catalogJson = "[]")
    {
        var mockStorage = new Mock<ILocalStorageService>();
        var mockLoader = new Mock<IIngredientCatalogLoader>();
        mockLoader.Setup(x => x.LoadCatalogJsonAsync()).ReturnsAsync(catalogJson);

        var vm = new IngredientsViewModel(mockStorage.Object, mockLoader.Object);
        return (vm, mockStorage, mockLoader);
    }

    /// Builds the VM and runs InitAsync so the catalog is loaded.
    private static async Task<(IngredientsViewModel vm,
                                Mock<ILocalStorageService> mockStorage,
                                Mock<IIngredientCatalogLoader> mockLoader)>
        BuildInitialisedVmAsync(string catalogJson = "[]")
    {
        var (vm, mockStorage, mockLoader) = BuildVm(catalogJson);
        await vm.InitAsync();
        return (vm, mockStorage, mockLoader);
    }

    // ── InitAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task InitAsync_PopulatesIngredientsByCategory_FromCatalogJson()
    {
        var json = CatalogJson(
            ("Chicken", IngredientCategory.Meat),
            ("Onion", IngredientCategory.Vegetables));

        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        Assert.Contains(vm.IngredientsByCategory[IngredientCategory.Meat],
            i => i.Name == "Chicken");
        Assert.Contains(vm.IngredientsByCategory[IngredientCategory.Vegetables],
            i => i.Name == "Onion");
    }

    [Fact]
    public async Task InitAsync_AllCategoryKeys_PresentEvenIfNotInJson()
    {
        // JSON only has Meat — all other enum values should still exist as empty lists
        var json = CatalogJson(("Chicken", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        foreach (IngredientCategory category in Enum.GetValues<IngredientCategory>())
            Assert.True(vm.IngredientsByCategory.ContainsKey(category),
                $"Missing key: {category}");
    }

    [Fact]
    public async Task InitAsync_SetsBinaryFlag_ForSeasoningsIngredients()
    {
        var json = CatalogJson(("Salt", IngredientCategory.Seasonings));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var salt = vm.IngredientsByCategory[IngredientCategory.Seasonings].First(i => i.Name == "Salt");
        Assert.True(salt.IsBinary);
    }

    [Fact]
    public async Task InitAsync_SetsBinaryFlag_ForOilsAndCondimentsIngredients()
    {
        var json = CatalogJson(("Olive Oil", IngredientCategory.OilsAndCondiments));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var oil = vm.IngredientsByCategory[IngredientCategory.OilsAndCondiments].First(i => i.Name == "Olive Oil");
        Assert.True(oil.IsBinary);
    }

    [Fact]
    public async Task InitAsync_DoesNotSetBinaryFlag_ForNonBinaryCategories()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var chicken = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Chicken");
        Assert.False(chicken.IsBinary);
    }

    [Fact]
    public async Task InitAsync_GroupsMultipleIngredientsIntoSameCategory()
    {
        var json = CatalogJson(
            ("Chicken", IngredientCategory.Meat),
            ("Beef", IngredientCategory.Meat),
            ("Pork", IngredientCategory.Meat));

        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        Assert.Equal(3, vm.IngredientsByCategory[IngredientCategory.Meat].Count);
    }

    [Fact]
    public async Task InitAsync_EmptyCatalog_AllCategoriesExistButEmpty()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync("[]");

        Assert.All(vm.IngredientsByCategory.Values, list => Assert.Empty(list));
    }

    // ── GetSelectedIngredients ───────────────────────────────────────────────

    [Fact]
    public async Task GetSelectedIngredients_ReturnsEmpty_WhenNoneSelected()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat), ("Beef", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        Assert.Empty(vm.GetSelectedIngredients());
    }

    [Fact]
    public async Task GetSelectedIngredients_ReturnsOnlySelectedIngredients()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat), ("Beef", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var chicken = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Chicken");
        vm.SelectIngredient(chicken);

        var result = vm.GetSelectedIngredients();
        Assert.Single(result);
        Assert.Contains("Chicken", result);
    }

    [Fact]
    public async Task GetSelectedIngredients_FormatsWithQuantity_WhenQuantityIsSet()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var chicken = vm.IngredientsByCategory[IngredientCategory.Meat].First();
        vm.SelectIngredient(chicken, quantity: "200g");

        Assert.Contains("Chicken (200g)", vm.GetSelectedIngredients());
    }

    [Fact]
    public async Task GetSelectedIngredients_FormatsWithoutQuantity_WhenQuantityIsEmpty()
    {
        var json = CatalogJson(("Pepper", IngredientCategory.Seasonings));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var pepper = vm.IngredientsByCategory[IngredientCategory.Seasonings].First();
        vm.SelectIngredient(pepper, quantity: "");

        var result = vm.GetSelectedIngredients();
        Assert.Contains("Pepper", result);
        Assert.DoesNotContain("()", result);
    }

    [Fact]
    public async Task GetSelectedIngredients_TrimsQuantityWhitespace()
    {
        var json = CatalogJson(("Carrot", IngredientCategory.Vegetables));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var carrot = vm.IngredientsByCategory[IngredientCategory.Vegetables].First();
        vm.SelectIngredient(carrot, quantity: "  100g  ");

        Assert.Contains("Carrot (100g)", vm.GetSelectedIngredients());
    }

    [Fact]
    public async Task GetSelectedIngredients_AggregatesAcrossCategories()
    {
        var json = CatalogJson(
            ("Chicken", IngredientCategory.Meat),
            ("Onion", IngredientCategory.Vegetables));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        vm.SelectIngredient(vm.IngredientsByCategory[IngredientCategory.Meat].First());
        vm.SelectIngredient(vm.IngredientsByCategory[IngredientCategory.Vegetables].First());

        var result = vm.GetSelectedIngredients();
        Assert.Equal(2, result.Count);
        Assert.Contains("Chicken", result);
        Assert.Contains("Onion", result);
    }

    // ── SelectIngredient / DeselectIngredient ────────────────────────────────

    [Fact]
    public async Task SelectIngredient_SetsIsSelectedTrue()
    {
        var json = CatalogJson(("Milk", IngredientCategory.Dairy));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var milk = vm.IngredientsByCategory[IngredientCategory.Dairy].First();
        vm.SelectIngredient(milk);

        Assert.True(milk.IsSelected);
    }

    [Fact]
    public async Task SelectIngredient_SetsQuantity()
    {
        var json = CatalogJson(("Milk", IngredientCategory.Dairy));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var milk = vm.IngredientsByCategory[IngredientCategory.Dairy].First();
        vm.SelectIngredient(milk, quantity: "500ml");

        Assert.Equal("500ml", milk.Quantity);
    }

    [Fact]
    public async Task SelectIngredient_SetsEmptyQuantity_WhenNoneProvided()
    {
        var json = CatalogJson(("Milk", IngredientCategory.Dairy));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var milk = vm.IngredientsByCategory[IngredientCategory.Dairy].First();
        vm.SelectIngredient(milk);

        Assert.Equal(string.Empty, milk.Quantity);
    }

    [Fact]
    public async Task SelectIngredient_TriggersStorageSave()
    {
        var json = CatalogJson(("Milk", IngredientCategory.Dairy));
        var (vm, mockStorage, _) = await BuildInitialisedVmAsync(json);

        var milk = vm.IngredientsByCategory[IngredientCategory.Dairy].First();
        vm.SelectIngredient(milk);

        mockStorage.Verify(x => x.SaveIngredientsAsync(vm), Times.Once);
    }

    [Fact]
    public async Task DeselectIngredient_SetsIsSelectedFalse()
    {
        var json = CatalogJson(("Milk", IngredientCategory.Dairy));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var milk = vm.IngredientsByCategory[IngredientCategory.Dairy].First();
        vm.SelectIngredient(milk, quantity: "500ml");
        vm.DeselectIngredient(milk);

        Assert.False(milk.IsSelected);
    }

    [Fact]
    public async Task DeselectIngredient_ClearsQuantity()
    {
        var json = CatalogJson(("Milk", IngredientCategory.Dairy));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var milk = vm.IngredientsByCategory[IngredientCategory.Dairy].First();
        vm.SelectIngredient(milk, quantity: "500ml");
        vm.DeselectIngredient(milk);

        Assert.Equal(string.Empty, milk.Quantity);
    }

    [Fact]
    public async Task DeselectIngredient_TriggersStorageSave()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat));
        var (vm, mockStorage, _) = await BuildInitialisedVmAsync(json);

        var chicken = vm.IngredientsByCategory[IngredientCategory.Meat].First();
        vm.DeselectIngredient(chicken);

        mockStorage.Verify(x => x.SaveIngredientsAsync(vm), Times.Once);
    }

    [Fact]
    public async Task DeselectIngredient_RemovedFromGetSelectedIngredients()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var chicken = vm.IngredientsByCategory[IngredientCategory.Meat].First();
        vm.SelectIngredient(chicken);
        vm.DeselectIngredient(chicken);

        Assert.Empty(vm.GetSelectedIngredients());
    }

    // ── ToggleBinary ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleBinary_TogglesFromFalseToTrue()
    {
        var json = CatalogJson(("Salt", IngredientCategory.Seasonings));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var salt = vm.IngredientsByCategory[IngredientCategory.Seasonings].First();
        vm.ToggleBinary(salt);

        Assert.True(salt.IsSelected);
    }

    [Fact]
    public async Task ToggleBinary_TogglesFromTrueToFalse()
    {
        var json = CatalogJson(("Salt", IngredientCategory.Seasonings));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        var salt = vm.IngredientsByCategory[IngredientCategory.Seasonings].First();
        vm.ToggleBinary(salt);
        vm.ToggleBinary(salt);

        Assert.False(salt.IsSelected);
    }

    [Fact]
    public async Task ToggleBinary_TriggersStorageSave()
    {
        var json = CatalogJson(("Salt", IngredientCategory.Seasonings));
        var (vm, mockStorage, _) = await BuildInitialisedVmAsync(json);

        var salt = vm.IngredientsByCategory[IngredientCategory.Seasonings].First();
        vm.ToggleBinary(salt);

        mockStorage.Verify(x => x.SaveIngredientsAsync(vm), Times.Once);
    }

    // ── AddCustomIngredient ──────────────────────────────────────────────────

    [Fact]
    public async Task AddCustomIngredient_AddsToCorrectCategory()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();

        vm.AddCustomIngredient("Tofu", IngredientCategory.Meat);

        Assert.Contains(vm.IngredientsByCategory[IngredientCategory.Meat], i => i.Name == "Tofu");
    }

    [Fact]
    public async Task AddCustomIngredient_SetsIsCustomTrue()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();

        vm.AddCustomIngredient("Tofu", IngredientCategory.Meat);

        var added = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        Assert.True(added.IsCustom);
    }

    [Fact]
    public async Task AddCustomIngredient_SetsIsSelectedFalse()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();

        vm.AddCustomIngredient("Tofu", IngredientCategory.Meat);

        var added = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        Assert.False(added.IsSelected);
    }

    [Fact]
    public async Task AddCustomIngredient_SetsBinaryFlag_ForSeasoningsCategory()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();

        vm.AddCustomIngredient("Custom Spice", IngredientCategory.Seasonings);

        var added = vm.IngredientsByCategory[IngredientCategory.Seasonings].First(i => i.Name == "Custom Spice");
        Assert.True(added.IsBinary);
    }

    [Fact]
    public async Task AddCustomIngredient_SetsBinaryFlag_ForOilsAndCondimentsCategory()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();

        vm.AddCustomIngredient("Truffle Oil", IngredientCategory.OilsAndCondiments);

        var added = vm.IngredientsByCategory[IngredientCategory.OilsAndCondiments].First(i => i.Name == "Truffle Oil");
        Assert.True(added.IsBinary);
    }

    [Fact]
    public async Task AddCustomIngredient_DoesNotSetBinaryFlag_ForNonBinaryCategory()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();

        vm.AddCustomIngredient("Tofu", IngredientCategory.Meat);

        var added = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        Assert.False(added.IsBinary);
    }

    [Fact]
    public async Task AddCustomIngredient_TrimsWhitespace()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();

        vm.AddCustomIngredient("  Tempeh  ", IngredientCategory.Meat);

        Assert.Contains(vm.IngredientsByCategory[IngredientCategory.Meat], i => i.Name == "Tempeh");
    }

    [Fact]
    public async Task AddCustomIngredient_IgnoresBlankInput()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();
        int before = vm.IngredientsByCategory[IngredientCategory.Meat].Count;

        vm.AddCustomIngredient("   ", IngredientCategory.Meat);

        Assert.Equal(before, vm.IngredientsByCategory[IngredientCategory.Meat].Count);
    }

    [Fact]
    public async Task AddCustomIngredient_IgnoresDuplicate_CaseInsensitive()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        vm.AddCustomIngredient("chicken", IngredientCategory.Meat);

        Assert.Equal(1, vm.IngredientsByCategory[IngredientCategory.Meat]
            .Count(i => i.Name.Equals("Chicken", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task AddCustomIngredient_IgnoresDuplicate_AcrossCategories()
    {
        var json = CatalogJson(("Egg", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);

        vm.AddCustomIngredient("Egg", IngredientCategory.Dairy);

        Assert.DoesNotContain(vm.IngredientsByCategory[IngredientCategory.Dairy], i => i.Name == "Egg");
    }

    [Fact]
    public async Task AddCustomIngredient_TriggersStorageSave()
    {
        var (vm, mockStorage, _) = await BuildInitialisedVmAsync();

        vm.AddCustomIngredient("Tofu", IngredientCategory.Meat);

        mockStorage.Verify(x => x.SaveIngredientsAsync(vm), Times.Once);
    }

    [Fact]
    public async Task AddCustomIngredient_DuplicateDoesNotTriggerStorageSave()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat));
        var (vm, mockStorage, _) = await BuildInitialisedVmAsync(json);

        vm.AddCustomIngredient("Chicken", IngredientCategory.Meat);

        mockStorage.Verify(x => x.SaveIngredientsAsync(It.IsAny<IngredientsViewModel>()), Times.Never);
    }

    // ── RemoveCustomIngredient ───────────────────────────────────────────────

    [Fact]
    public async Task RemoveCustomIngredient_RemovesFromCategory()
    {
        var (vm, _, _) = await BuildInitialisedVmAsync();
        vm.AddCustomIngredient("Tofu", IngredientCategory.Meat);

        var tofu = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        vm.RemoveCustomIngredient(tofu);

        Assert.DoesNotContain(vm.IngredientsByCategory[IngredientCategory.Meat], i => i.Name == "Tofu");
    }

    [Fact]
    public async Task RemoveCustomIngredient_TriggersStorageSave()
    {
        var (vm, mockStorage, _) = await BuildInitialisedVmAsync();
        vm.AddCustomIngredient("Tofu", IngredientCategory.Meat);
        mockStorage.Invocations.Clear();

        var tofu = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        vm.RemoveCustomIngredient(tofu);

        mockStorage.Verify(x => x.SaveIngredientsAsync(vm), Times.Once);
    }

    [Fact]
    public async Task RemoveCustomIngredient_DoesNotAffectOtherIngredients()
    {
        var json = CatalogJson(("Chicken", IngredientCategory.Meat));
        var (vm, _, _) = await BuildInitialisedVmAsync(json);
        vm.AddCustomIngredient("Tofu", IngredientCategory.Meat);

        var tofu = vm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Tofu");
        vm.RemoveCustomIngredient(tofu);

        Assert.Contains(vm.IngredientsByCategory[IngredientCategory.Meat], i => i.Name == "Chicken");
    }
}
