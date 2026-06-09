using Moq;
using VibeCooking.Models;
using VibeCooking.Services;
using VibeCooking.ViewModels;

namespace VibeCooking.Tests.Tests;

public class MyRecipesViewModelTests
{
    private static MyRecipesViewModel BuildVm(
        ILocalStorageService? storage = null,
        List<SavedRecipeModel>? initialRecipes = null)
    {
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage
            .Setup(x => x.LoadRecipesAsync())
            .ReturnsAsync(initialRecipes ?? new List<SavedRecipeModel>());

        var actualStorage = storage ?? mockStorage.Object;

        // RecipeChatViewModel needs its own deps wired up
        var mockApi = new Mock<IApiService>();
        var ingredientsVm = new IngredientsViewModel(
            new Mock<ILocalStorageService>().Object,
            new Mock<IIngredientCatalogLoader>().Object);
        var recipeGenVm = new RecipeGenerationViewModel(mockApi.Object, ingredientsVm);
        var chatVm = new RecipeChatViewModel(mockApi.Object, recipeGenVm, actualStorage);

        return new MyRecipesViewModel(actualStorage, chatVm);
    }

    private static SavedRecipeModel MakeRecipe(
        string saveName, string cuisine = "Italian",
        string mealType = "Dinner", string difficulty = "Easy") =>
        new()
        {
            SaveName = saveName,
            Recipe = new RecipeOutputModel
            {
                CuisineType = cuisine,
                MealType = mealType,
                Difficulty = difficulty
            }
        };

    // ──────────────────────────────────────────
    // FilteredRecipes
    // ──────────────────────────────────────────

    [Fact]
    public async Task FilteredRecipes_EmptyQuery_ReturnsAll()
    {
        var recipes = new List<SavedRecipeModel>
        {
            MakeRecipe("Pasta"),
            MakeRecipe("Pizza")
        };
        var vm = BuildVm(initialRecipes: recipes);
        await vm.InitAsync();

        vm.SearchQuery = string.Empty;
        Assert.Equal(2, vm.FilteredRecipes.Count);
    }

    [Fact]
    public async Task FilteredRecipes_ByName_ReturnsMatch()
    {
        var recipes = new List<SavedRecipeModel>
        {
            MakeRecipe("Spicy Pasta"),
            MakeRecipe("Plain Rice")
        };
        var vm = BuildVm(initialRecipes: recipes);
        await vm.InitAsync();

        vm.SearchQuery = "Spicy";
        Assert.Single(vm.FilteredRecipes);
        Assert.Equal("Spicy Pasta", vm.FilteredRecipes[0].SaveName);
    }

    [Fact]
    public async Task FilteredRecipes_ByCuisineType_ReturnsMatch()
    {
        var recipes = new List<SavedRecipeModel>
        {
            MakeRecipe("Dish A", cuisine: "Japanese"),
            MakeRecipe("Dish B", cuisine: "Italian")
        };
        var vm = BuildVm(initialRecipes: recipes);
        await vm.InitAsync();

        vm.SearchQuery = "Japanese";
        Assert.Single(vm.FilteredRecipes);
        Assert.Equal("Dish A", vm.FilteredRecipes[0].SaveName);
    }

    [Fact]
    public async Task FilteredRecipes_ByMealType_ReturnsMatch()
    {
        var recipes = new List<SavedRecipeModel>
        {
            MakeRecipe("Omelette", mealType: "Breakfast"),
            MakeRecipe("Steak", mealType: "Dinner")
        };
        var vm = BuildVm(initialRecipes: recipes);
        await vm.InitAsync();

        vm.SearchQuery = "Breakfast";
        Assert.Single(vm.FilteredRecipes);
        Assert.Equal("Omelette", vm.FilteredRecipes[0].SaveName);
    }

    [Fact]
    public async Task FilteredRecipes_ByDifficulty_ReturnsMatch()
    {
        var recipes = new List<SavedRecipeModel>
        {
            MakeRecipe("Simple Salad", difficulty: "Easy"),
            MakeRecipe("Beef Wellington", difficulty: "Hard")
        };
        var vm = BuildVm(initialRecipes: recipes);
        await vm.InitAsync();

        vm.SearchQuery = "Hard";
        Assert.Single(vm.FilteredRecipes);
        Assert.Equal("Beef Wellington", vm.FilteredRecipes[0].SaveName);
    }

    [Fact]
    public async Task FilteredRecipes_IsCaseInsensitive()
    {
        var recipes = new List<SavedRecipeModel> { MakeRecipe("Chicken Tikka") };
        var vm = BuildVm(initialRecipes: recipes);
        await vm.InitAsync();

        vm.SearchQuery = "chicken tikka";
        Assert.Single(vm.FilteredRecipes);
    }

    [Fact]
    public async Task FilteredRecipes_NoMatch_ReturnsEmpty()
    {
        var recipes = new List<SavedRecipeModel> { MakeRecipe("Pasta") };
        var vm = BuildVm(initialRecipes: recipes);
        await vm.InitAsync();

        vm.SearchQuery = "XYZ_NO_MATCH";
        Assert.Empty(vm.FilteredRecipes);
    }

    // ──────────────────────────────────────────
    // DeleteRecipeAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task DeleteRecipeAsync_CallsDeleteOnStorageWithCorrectId()
    {
        var recipe = MakeRecipe("Test Recipe");
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage.Setup(x => x.LoadRecipesAsync()).ReturnsAsync(new List<SavedRecipeModel> { recipe });

        var vm = BuildVm(storage: mockStorage.Object);
        await vm.InitAsync();
        await vm.DeleteRecipeAsync(recipe);

        mockStorage.Verify(x => x.DeleteRecipeAsync(recipe.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteRecipeAsync_RefreshesListAfterDeletion()
    {
        var recipe = MakeRecipe("Test Recipe");
        var mockStorage = new Mock<ILocalStorageService>();

        // First call returns the recipe, second call (after deletion) returns empty
        var callCount = 0;
        mockStorage
            .Setup(x => x.LoadRecipesAsync())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new List<SavedRecipeModel> { recipe }
                    : new List<SavedRecipeModel>();
            });

        var vm = BuildVm(storage: mockStorage.Object);
        await vm.InitAsync();
        await vm.DeleteRecipeAsync(recipe);

        Assert.Empty(vm.Recipes);
    }

    // ──────────────────────────────────────────
    // UpdateRecipeImageAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task UpdateRecipeImageAsync_SavesUpdatedRecipeToStorage()
    {
        var recipe = MakeRecipe("Photo Test");
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage.Setup(x => x.LoadRecipesAsync()).ReturnsAsync(new List<SavedRecipeModel> { recipe });

        var vm = BuildVm(storage: mockStorage.Object);
        await vm.InitAsync();

        await vm.UpdateRecipeImageAsync(recipe.Id, "base64data", "image/jpeg");

        mockStorage.Verify(x => x.SaveRecipeAsync(It.Is<SavedRecipeModel>(r =>
            r.Id == recipe.Id &&
            r.ImageBase64 == "base64data" &&
            r.ImageMimeType == "image/jpeg"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateRecipeImageAsync_UnknownId_DoesNotCallStorage()
    {
        var recipe = MakeRecipe("Test");
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage.Setup(x => x.LoadRecipesAsync()).ReturnsAsync(new List<SavedRecipeModel> { recipe });

        var vm = BuildVm(storage: mockStorage.Object);
        await vm.InitAsync();

        await vm.UpdateRecipeImageAsync(Guid.NewGuid(), "data", "image/png");

        mockStorage.Verify(x => x.SaveRecipeAsync(It.IsAny<SavedRecipeModel>()), Times.Never);
    }

    // ──────────────────────────────────────────
    // OpenRecipe
    // ──────────────────────────────────────────

    [Fact]
    public async Task OpenRecipe_CallsLoadRecipeOnChatViewModel_WithCorrectRecipe()
    {
        var recipe = MakeRecipe("Loaded Recipe");
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage.Setup(x => x.LoadRecipesAsync()).ReturnsAsync(new List<SavedRecipeModel> { recipe });

        // Build the chat VM separately so we can inspect its state after OpenRecipe
        var mockApi = new Mock<IApiService>();
        var ingredientsVm = new IngredientsViewModel(
            new Mock<ILocalStorageService>().Object,
            new Mock<IIngredientCatalogLoader>().Object);
        var recipeGenVm = new RecipeGenerationViewModel(mockApi.Object, ingredientsVm);
        var chatVm = new RecipeChatViewModel(mockApi.Object, recipeGenVm, mockStorage.Object);
        var vm = new MyRecipesViewModel(mockStorage.Object, chatVm);

        await vm.InitAsync();
        vm.OpenRecipe(recipe);

        // LoadRecipe sets CurrentRecipe on the chat VM — verify the right recipe was passed
        Assert.Same(recipe.Recipe, chatVm.CurrentRecipe);
    }

    [Fact]
    public async Task OpenRecipe_ClearsPreviousChatHistory()
    {
        var recipe = MakeRecipe("Test");
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage.Setup(x => x.LoadRecipesAsync()).ReturnsAsync(new List<SavedRecipeModel> { recipe });

        var mockApi = new Mock<IApiService>();
        var ingredientsVm = new IngredientsViewModel(
            new Mock<ILocalStorageService>().Object,
            new Mock<IIngredientCatalogLoader>().Object);
        var recipeGenVm = new RecipeGenerationViewModel(mockApi.Object, ingredientsVm);
        var chatVm = new RecipeChatViewModel(mockApi.Object, recipeGenVm, mockStorage.Object);
        var vm = new MyRecipesViewModel(mockStorage.Object, chatVm);

        await vm.InitAsync();

        // Simulate prior chat history
        chatVm.Messages.Add(new VibeCooking.Models.ChatMessageModel { Role = "user", Content = "old message" });

        vm.OpenRecipe(recipe);

        Assert.Empty(chatVm.Messages);
    }
}
