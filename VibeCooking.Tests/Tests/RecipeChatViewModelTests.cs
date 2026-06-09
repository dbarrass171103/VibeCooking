using Moq;
using VibeCooking.Models;
using VibeCooking.Services;
using VibeCooking.ViewModels;

namespace VibeCooking.Tests.Tests;

public class RecipeChatViewModelTests
{
    private static readonly RecipeOutputModel SampleRecipe = new()
    {
        RecipeName = "Chicken Pasta",
        Description = "A simple pasta dish",
        CuisineType = "Italian",
        Difficulty = "Easy",
        MealType = "Dinner",
        CookTimeMinutes = 30,
        PrepTimeMinutes = 10,
        Servings = 4,
        Ingredients = [new IngredientModel { Name = "Chicken", Quantity = "200g" }]
    };

    private static (RecipeChatViewModel vm, Mock<IApiService> mockApi, Mock<ILocalStorageService> mockStorage)
        BuildVm(RecipeOutputModel? generatedRecipe = null)
    {
        var mockApi = new Mock<IApiService>();
        var mockStorage = new Mock<ILocalStorageService>();
        var ingredientsVm = new IngredientsViewModel(
            new Mock<ILocalStorageService>().Object,
            new Mock<IIngredientCatalogLoader>().Object);
        var recipeGenVm = new RecipeGenerationViewModel(mockApi.Object, ingredientsVm);

        // Simulate a recipe having been generated so InitAsync has something to load
        if (generatedRecipe is not null)
            recipeGenVm.RecipeCards.Add(new RecipeCardModel());

        // Inject the generated recipe directly via reflection so we can control it
        if (generatedRecipe is not null)
        {
            var prop = typeof(RecipeGenerationViewModel)
                .GetProperty(nameof(RecipeGenerationViewModel.GeneratedRecipe));
            prop?.SetValue(recipeGenVm, generatedRecipe);
        }

        var vm = new RecipeChatViewModel(mockApi.Object, recipeGenVm, mockStorage.Object);
        return (vm, mockApi, mockStorage);
    }

    // ──────────────────────────────────────────
    // InitAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task InitAsync_LoadsGeneratedRecipe_FromRecipeGenerationViewModel()
    {
        var (vm, _, _) = BuildVm(generatedRecipe: SampleRecipe);

        await vm.InitAsync();

        Assert.NotNull(vm.CurrentRecipe);
        Assert.Equal("Chicken Pasta", vm.CurrentRecipe!.RecipeName);
    }

    [Fact]
    public async Task InitAsync_ClearsExistingMessages()
    {
        var (vm, _, _) = BuildVm(generatedRecipe: SampleRecipe);
        await vm.InitAsync();

        // Simulate some messages already present from a previous session
        vm.Messages.Add(new ChatMessageModel { Role = "user", Content = "old message" });

        await vm.InitAsync();

        Assert.Empty(vm.Messages);
    }

    [Fact]
    public async Task InitAsync_ClearsErrorMessage()
    {
        var (vm, _, _) = BuildVm(generatedRecipe: SampleRecipe);
        await vm.InitAsync();

        // Simulate a prior error
        var errorProp = typeof(RecipeChatViewModel).GetProperty("ErrorMessage");
        errorProp?.SetValue(vm, "Some old error");

        await vm.InitAsync();

        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task InitAsync_DoesNotOverwriteRecipe_WhenLoadedFromSaved()
    {
        var (vm, _, _) = BuildVm(generatedRecipe: SampleRecipe);
        await vm.InitAsync();

        var savedRecipe = new RecipeOutputModel { RecipeName = "Saved Soup" };
        vm.LoadRecipe(savedRecipe);

        // InitAsync should skip overwriting because LoadRecipe set the flag
        await vm.InitAsync();

        Assert.Equal("Saved Soup", vm.CurrentRecipe!.RecipeName);
    }

    [Fact]
    public async Task InitAsync_AfterLoadRecipe_ClearsLoadedFlag_SoSubsequentInitReloads()
    {
        var (vm, _, _) = BuildVm(generatedRecipe: SampleRecipe);
        await vm.InitAsync();

        vm.LoadRecipe(new RecipeOutputModel { RecipeName = "Saved Soup" });
        await vm.InitAsync(); // consumes the flag

        // Now a fresh InitAsync should reload from generation
        await vm.InitAsync();

        Assert.Equal("Chicken Pasta", vm.CurrentRecipe!.RecipeName);
    }

    // ──────────────────────────────────────────
    // LoadRecipe
    // ──────────────────────────────────────────

    [Fact]
    public void LoadRecipe_SetsCurrentRecipe()
    {
        var (vm, _, _) = BuildVm();
        var recipe = new RecipeOutputModel { RecipeName = "Loaded Recipe" };

        vm.LoadRecipe(recipe);

        Assert.Equal("Loaded Recipe", vm.CurrentRecipe!.RecipeName);
    }

    [Fact]
    public void LoadRecipe_ClearsMessages()
    {
        var (vm, _, _) = BuildVm();
        vm.Messages.Add(new ChatMessageModel { Role = "user", Content = "old" });

        vm.LoadRecipe(new RecipeOutputModel());

        Assert.Empty(vm.Messages);
    }

    [Fact]
    public void LoadRecipe_IsReady_ReturnsTrueAfterLoad()
    {
        var (vm, _, _) = BuildVm();
        Assert.False(vm.IsReady);

        vm.LoadRecipe(new RecipeOutputModel());

        Assert.True(vm.IsReady);
    }

    // ──────────────────────────────────────────
    // SendMessageAsync — guards
    // ──────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_DoesNothing_WhenMessageIsWhitespace()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        await vm.SendMessageAsync("   ");

        mockApi.Verify(x => x.SendChatMessageAsync(
            It.IsAny<List<ChatMessageModel>>(),
            It.IsAny<RecipeOutputModel>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_DoesNothing_WhenCurrentRecipeIsNull()
    {
        var (vm, mockApi, _) = BuildVm();
        // Don't load a recipe — CurrentRecipe stays null

        await vm.SendMessageAsync("What temperature should I use?");

        mockApi.Verify(x => x.SendChatMessageAsync(
            It.IsAny<List<ChatMessageModel>>(),
            It.IsAny<RecipeOutputModel>(),
            It.IsAny<string>()), Times.Never);
    }

    // ──────────────────────────────────────────
    // SendMessageAsync — success path
    // ──────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_AddsUserMessageToHistory_ExactlyOnce()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        var updatedRecipe = new RecipeOutputModel { RecipeName = "Updated Pasta" };
        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, string.Empty, new ChatResponseModel
            {
                Reply = "Sure!",
                Recipe = updatedRecipe
            }));

        await vm.SendMessageAsync("Make it spicy");

        Assert.Single(vm.Messages, m => m.Role == "user");
        Assert.Equal("Make it spicy", vm.Messages.First(m => m.Role == "user").Content);
    }

    [Fact]
    public async Task SendMessageAsync_TrimsUserMessage()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, string.Empty, new ChatResponseModel
            {
                Reply = "Done",
                Recipe = SampleRecipe
            }));

        await vm.SendMessageAsync("  Make it spicy  ");

        var userMsg = vm.Messages.First(m => m.Role == "user");
        Assert.Equal("Make it spicy", userMsg.Content);
    }

    [Fact]
    public async Task SendMessageAsync_OnSuccess_AddsAssistantReplyToHistory()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, string.Empty, new ChatResponseModel
            {
                Reply = "I've made it spicier!",
                Recipe = SampleRecipe
            }));

        await vm.SendMessageAsync("Make it spicy");

        Assert.Single(vm.Messages, m => m.Role == "assistant");
        Assert.Equal("I've made it spicier!", vm.Messages.First(m => m.Role == "assistant").Content);
    }

    [Fact]
    public async Task SendMessageAsync_OnSuccess_UpdatesCurrentRecipe()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        var updatedRecipe = new RecipeOutputModel { RecipeName = "Spicy Chicken Pasta" };
        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, string.Empty, new ChatResponseModel
            {
                Reply = "Done!",
                Recipe = updatedRecipe
            }));

        await vm.SendMessageAsync("Make it spicy");

        Assert.Equal("Spicy Chicken Pasta", vm.CurrentRecipe!.RecipeName);
    }

    [Fact]
    public async Task SendMessageAsync_OnSuccess_ErrorMessageRemainsNull()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, string.Empty, new ChatResponseModel
            {
                Reply = "Done",
                Recipe = SampleRecipe
            }));

        await vm.SendMessageAsync("Any question");

        Assert.Null(vm.ErrorMessage);
    }

    // ──────────────────────────────────────────
    // SendMessageAsync — failure path
    // ──────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_OnApiFailure_SetsErrorMessage()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((false, "Service unavailable", null));

        await vm.SendMessageAsync("Any question");

        Assert.Equal("Service unavailable", vm.ErrorMessage);
    }

    [Fact]
    public async Task SendMessageAsync_OnApiFailure_RemovesUserMessageFromHistory()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((false, "Error", null));

        await vm.SendMessageAsync("Any question");

        Assert.Empty(vm.Messages);
    }

    [Fact]
    public async Task SendMessageAsync_OnApiFailure_DoesNotUpdateRecipe()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((false, "Error", null));

        await vm.SendMessageAsync("Any question");

        Assert.Equal("Chicken Pasta", vm.CurrentRecipe!.RecipeName);
    }

    [Fact]
    public async Task SendMessageAsync_MultiTurn_HistoryGrowsCorrectly()
    {
        var (vm, mockApi, _) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        mockApi
            .Setup(x => x.SendChatMessageAsync(
                It.IsAny<List<ChatMessageModel>>(),
                It.IsAny<RecipeOutputModel>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, string.Empty, new ChatResponseModel
            {
                Reply = "Reply",
                Recipe = SampleRecipe
            }));

        await vm.SendMessageAsync("First question");
        await vm.SendMessageAsync("Second question");

        // 2 user messages + 2 assistant replies
        Assert.Equal(4, vm.Messages.Count);
        Assert.Equal(2, vm.Messages.Count(m => m.Role == "user"));
        Assert.Equal(2, vm.Messages.Count(m => m.Role == "assistant"));
    }

    // ──────────────────────────────────────────
    // SaveRecipeAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task SaveRecipeAsync_DoesNothing_WhenCurrentRecipeIsNull()
    {
        var (vm, _, mockStorage) = BuildVm();
        // Don't load a recipe

        await vm.SaveRecipeAsync("My Recipe");

        mockStorage.Verify(x => x.SaveRecipeAsync(It.IsAny<SavedRecipeModel>()), Times.Never);
    }

    [Fact]
    public async Task SaveRecipeAsync_CallsStorageWithCorrectSaveName()
    {
        var (vm, _, mockStorage) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        await vm.SaveRecipeAsync("My Favourite Pasta");

        mockStorage.Verify(x => x.SaveRecipeAsync(
            It.Is<SavedRecipeModel>(r => r.SaveName == "My Favourite Pasta")),
            Times.Once);
    }

    [Fact]
    public async Task SaveRecipeAsync_BuildsCardWithCorrectFieldsFromRecipe()
    {
        var (vm, _, mockStorage) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        SavedRecipeModel? captured = null;
        mockStorage
            .Setup(x => x.SaveRecipeAsync(It.IsAny<SavedRecipeModel>()))
            .Callback<SavedRecipeModel>(r => captured = r)
            .Returns(Task.CompletedTask);

        await vm.SaveRecipeAsync("Test Save");

        Assert.NotNull(captured);
        Assert.Equal("Chicken Pasta", captured!.Card.RecipeName);
        Assert.Equal("Italian", captured.Card.CuisineType);
        Assert.Equal("Easy", captured.Card.Difficulty);
        Assert.Equal("Dinner", captured.Card.MealType);
        Assert.Equal(30, captured.Card.CookTimeMinutes);
        Assert.Equal(4, captured.Card.Servings);
        Assert.Contains("Chicken", captured.Card.Ingredients);
    }

    [Fact]
    public async Task SaveRecipeAsync_StoresImageData_WhenProvided()
    {
        var (vm, _, mockStorage) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        SavedRecipeModel? captured = null;
        mockStorage
            .Setup(x => x.SaveRecipeAsync(It.IsAny<SavedRecipeModel>()))
            .Callback<SavedRecipeModel>(r => captured = r)
            .Returns(Task.CompletedTask);

        await vm.SaveRecipeAsync("Test", imageBase64: "abc123", imageMimeType: "image/jpeg");

        Assert.Equal("abc123", captured!.ImageBase64);
        Assert.Equal("image/jpeg", captured.ImageMimeType);
    }

    [Fact]
    public async Task SaveRecipeAsync_StoresFullRecipeObject()
    {
        var (vm, _, mockStorage) = BuildVm();
        vm.LoadRecipe(SampleRecipe);

        SavedRecipeModel? captured = null;
        mockStorage
            .Setup(x => x.SaveRecipeAsync(It.IsAny<SavedRecipeModel>()))
            .Callback<SavedRecipeModel>(r => captured = r)
            .Returns(Task.CompletedTask);

        await vm.SaveRecipeAsync("Test");

        Assert.Same(SampleRecipe, captured!.Recipe);
    }
}
