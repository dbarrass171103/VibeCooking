using Moq;
using VibeCooking.Models;
using VibeCooking.Services;
using VibeCooking.ViewModels;

namespace VibeCooking.Tests.Tests;

public class RecipeGenerationViewModelTests
{
    private static RecipeGenerationViewModel BuildVm(
        IApiService? apiService = null,
        IngredientsViewModel? ingredientsVm = null)
    {
        apiService ??= new Mock<IApiService>().Object;
        ingredientsVm ??= new IngredientsViewModel(
            new Mock<ILocalStorageService>().Object,
            new Mock<IIngredientCatalogLoader>().Object);
        return new RecipeGenerationViewModel(apiService, ingredientsVm);
    }

    // ──────────────────────────────────────────
    // AddCustomAllergen
    // ──────────────────────────────────────────

    [Fact]
    public void AddCustomAllergen_AddsNewAllergenToList()
    {
        var vm = BuildVm();
        vm.AddCustomAllergen("Shellfish");
        Assert.Contains("Shellfish", vm.CustomAllergens);
    }

    [Fact]
    public void AddCustomAllergen_TrimsWhitespace()
    {
        var vm = BuildVm();
        vm.AddCustomAllergen("  Shellfish  ");
        Assert.Contains("Shellfish", vm.CustomAllergens);
    }

    [Fact]
    public void AddCustomAllergen_IgnoresBlankInput()
    {
        var vm = BuildVm();
        vm.AddCustomAllergen("   ");
        Assert.Empty(vm.CustomAllergens);
    }

    [Fact]
    public void AddCustomAllergen_IgnoresDuplicateInCustomList_CaseInsensitive()
    {
        var vm = BuildVm();
        vm.AddCustomAllergen("Shellfish");
        vm.AddCustomAllergen("shellfish");
        Assert.Single(vm.CustomAllergens);
    }

    [Fact]
    public void AddCustomAllergen_IgnoresDuplicateInPresetList_CaseInsensitive()
    {
        var vm = BuildVm();
        // "Gluten" is in the preset list
        vm.AddCustomAllergen("gluten");
        Assert.Empty(vm.CustomAllergens);
    }

    // ──────────────────────────────────────────
    // RemoveCustomAllergen
    // ──────────────────────────────────────────

    [Fact]
    public void RemoveCustomAllergen_RemovesFromList()
    {
        var vm = BuildVm();
        vm.AddCustomAllergen("Shellfish");
        vm.RemoveCustomAllergen("Shellfish");
        Assert.Empty(vm.CustomAllergens);
    }

    [Fact]
    public void RemoveCustomAllergen_NonExistent_DoesNotThrow()
    {
        var vm = BuildVm();
        var ex = Record.Exception(() => vm.RemoveCustomAllergen("NonExistent"));
        Assert.Null(ex);
    }

    // ──────────────────────────────────────────
    // TogglePresetAllergen
    // ──────────────────────────────────────────

    [Fact]
    public void TogglePresetAllergen_AddsWhenNotPresent()
    {
        var vm = BuildVm();
        vm.TogglePresetAllergen("Gluten");
        Assert.Contains("Gluten", vm.Parameters.Allergies);
    }

    [Fact]
    public void TogglePresetAllergen_RemovesWhenAlreadyPresent()
    {
        var vm = BuildVm();
        vm.TogglePresetAllergen("Gluten");
        vm.TogglePresetAllergen("Gluten");
        Assert.DoesNotContain("Gluten", vm.Parameters.Allergies);
    }

    [Fact]
    public void TogglePresetAllergen_MultipleToggles_EndsInOriginalState()
    {
        var vm = BuildVm();
        for (int i = 0; i < 4; i++)
            vm.TogglePresetAllergen("Eggs");
        Assert.DoesNotContain("Eggs", vm.Parameters.Allergies);
    }

    // ──────────────────────────────────────────
    // IsAllergenSelected
    // ──────────────────────────────────────────

    [Fact]
    public void IsAllergenSelected_ReturnsTrueWhenPresent()
    {
        var vm = BuildVm();
        vm.TogglePresetAllergen("Nuts");
        Assert.True(vm.IsAllergenSelected("Nuts"));
    }

    [Fact]
    public void IsAllergenSelected_IsCaseInsensitive()
    {
        var vm = BuildVm();
        vm.TogglePresetAllergen("Nuts");
        Assert.True(vm.IsAllergenSelected("nuts"));
        Assert.True(vm.IsAllergenSelected("NUTS"));
    }

    [Fact]
    public void IsAllergenSelected_ReturnsFalseWhenNotPresent()
    {
        var vm = BuildVm();
        Assert.False(vm.IsAllergenSelected("Gluten"));
    }

    // ──────────────────────────────────────────
    // GenerateCardsAsync — allergen merging
    // ──────────────────────────────────────────

    [Fact]
    public async Task GenerateCardsAsync_MergesPresetAndCustomAllergens()
    {
        var capturedParams = new List<RecipeParameterModel>();

        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeCardsAsync(It.IsAny<RecipeParameterModel>()))
            .Callback<RecipeParameterModel>(p => capturedParams.Add(p))
            .ReturnsAsync((false, "test", new List<RecipeCardModel>()));

        var vm = BuildVm(mockApi.Object);

        // Toggle a preset allergen and add a genuinely custom one ("Shellfish" is not in PresetAllergens)
        vm.TogglePresetAllergen("Gluten");
        vm.AddCustomAllergen("Shellfish");

        await vm.GenerateCardsAsync();

        var sentAllergies = capturedParams.Single().Allergies;
        Assert.Contains("Gluten", sentAllergies);
        Assert.Contains("Shellfish", sentAllergies);
    }

    [Fact]
    public async Task GenerateCardsAsync_DeduplicatesAllergens()
    {
        var capturedParams = new List<RecipeParameterModel>();

        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeCardsAsync(It.IsAny<RecipeParameterModel>()))
            .Callback<RecipeParameterModel>(p => capturedParams.Add(p))
            .ReturnsAsync((false, "test", new List<RecipeCardModel>()));

        var vm = BuildVm(mockApi.Object);

        // Manually place the same entry into both Parameters.Allergies and CustomAllergens
        // to validate that GetAllAllergens deduplicates them
        vm.Parameters.Allergies.Add("Shellfish");
        vm.CustomAllergens.Add("shellfish"); // lowercase duplicate

        await vm.GenerateCardsAsync();

        var sentAllergies = capturedParams.Single().Allergies;
        Assert.Equal(1, sentAllergies.Count(a => a.Equals("Shellfish", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task GenerateCardsAsync_PassesSelectedIngredients()
    {
        var capturedParams = new List<RecipeParameterModel>();

        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeCardsAsync(It.IsAny<RecipeParameterModel>()))
            .Callback<RecipeParameterModel>(p => capturedParams.Add(p))
            .ReturnsAsync((false, "test", new List<RecipeCardModel>()));

        var mockLoader = new Mock<IIngredientCatalogLoader>();
        mockLoader.Setup(x => x.LoadCatalogJsonAsync())
            .ReturnsAsync("""[{"name":"Chicken","category":"Meat"},{"name":"Rice","category":"Grains"}]""");

        var ingredientsVm = new IngredientsViewModel(
            new Mock<ILocalStorageService>().Object, mockLoader.Object);
        await ingredientsVm.InitAsync();

        // Select both catalog ingredients
        ingredientsVm.SelectIngredient(ingredientsVm.IngredientsByCategory[IngredientCategory.Meat].First(i => i.Name == "Chicken"));
        ingredientsVm.SelectIngredient(ingredientsVm.IngredientsByCategory[IngredientCategory.Grains].First(i => i.Name == "Rice"));

        var vm = BuildVm(mockApi.Object, ingredientsVm);
        await vm.GenerateCardsAsync();

        Assert.Contains("Chicken", capturedParams.Single().Ingredients);
        Assert.Contains("Rice", capturedParams.Single().Ingredients);
    }

    [Fact]
    public async Task GenerateCardsAsync_OnApiSuccess_PopulatesRecipeCards()
    {
        var cards = new List<RecipeCardModel>
        {
            new() { RecipeName = "Pasta", Difficulty = "Easy" },
            new() { RecipeName = "Soup", Difficulty = "Medium" }
        };

        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeCardsAsync(It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((true, string.Empty, cards));

        var vm = BuildVm(mockApi.Object);
        await vm.GenerateCardsAsync();

        Assert.Equal(2, vm.RecipeCards.Count);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task GenerateCardsAsync_OnApiFailure_SetsErrorMessage()
    {
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeCardsAsync(It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((false, "Service unavailable", new List<RecipeCardModel>()));

        var vm = BuildVm(mockApi.Object);
        await vm.GenerateCardsAsync();

        Assert.Equal("Service unavailable", vm.ErrorMessage);
        Assert.Empty(vm.RecipeCards);
    }

    // ──────────────────────────────────────────
    // SelectCardAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task SelectCardAsync_OnSuccess_SetsGeneratedRecipe()
    {
        var recipe = new RecipeOutputModel { RecipeName = "Spicy Ramen" };
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((true, string.Empty, recipe));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(new RecipeCardModel { RecipeName = "Spicy Ramen" });

        Assert.Equal("Spicy Ramen", vm.GeneratedRecipe?.RecipeName);
    }

    [Fact]
    public async Task SelectCardAsync_OnSuccess_SetsSelectedCard()
    {
        var card = new RecipeCardModel { RecipeName = "Spicy Ramen" };
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((true, string.Empty, new RecipeOutputModel()));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(card);

        Assert.Same(card, vm.SelectedCard);
    }

    [Fact]
    public async Task SelectCardAsync_OnSuccess_ErrorMessageIsNull()
    {
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((true, string.Empty, new RecipeOutputModel()));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(new RecipeCardModel());

        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task SelectCardAsync_OnSuccess_IsGeneratingRecipeIsFalse()
    {
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((true, string.Empty, new RecipeOutputModel()));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(new RecipeCardModel());

        Assert.False(vm.IsGeneratingRecipe);
    }

    [Fact]
    public async Task SelectCardAsync_OnFailure_SetsErrorMessage()
    {
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((false, "Generation failed", null));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(new RecipeCardModel());

        Assert.Equal("Generation failed", vm.ErrorMessage);
    }

    [Fact]
    public async Task SelectCardAsync_OnFailure_ClearsSelectedCard()
    {
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((false, "Generation failed", null));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(new RecipeCardModel());

        Assert.Null(vm.SelectedCard);
    }

    [Fact]
    public async Task SelectCardAsync_OnFailure_GeneratedRecipeRemainsNull()
    {
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((false, "Generation failed", null));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(new RecipeCardModel());

        Assert.Null(vm.GeneratedRecipe);
    }

    [Fact]
    public async Task SelectCardAsync_OnFailure_IsGeneratingRecipeIsFalse()
    {
        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .ReturnsAsync((false, "Generation failed", null));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(new RecipeCardModel());

        Assert.False(vm.IsGeneratingRecipe);
    }

    [Fact]
    public async Task SelectCardAsync_PassesCorrectCardToApi()
    {
        var card = new RecipeCardModel { RecipeName = "Sushi", CookTimeMinutes = 45 };
        RecipeCardModel? captured = null;

        var mockApi = new Mock<IApiService>();
        mockApi
            .Setup(x => x.GenerateRecipeAsync(It.IsAny<RecipeCardModel>(), It.IsAny<RecipeParameterModel>()))
            .Callback<RecipeCardModel, RecipeParameterModel>((c, _) => captured = c)
            .ReturnsAsync((true, string.Empty, new RecipeOutputModel()));

        var vm = BuildVm(mockApi.Object);
        await vm.SelectCardAsync(card);

        Assert.Same(card, captured);
    }
}
