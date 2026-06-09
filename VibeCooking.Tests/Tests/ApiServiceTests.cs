using VibeCooking.Models;
using VibeCooking.Services;

namespace VibeCooking.Tests.Tests;

public class ApiServiceTests
{
    // ──────────────────────────────────────────
    // CleanJson
    // ──────────────────────────────────────────

    [Fact]
    public void CleanJson_RemovesOpeningMarkdownFence()
    {
        string raw = "```json\n{\"key\":\"value\"}";
        string result = ApiService.CleanJson(raw);
        Assert.DoesNotContain("```json", result);
    }

    [Fact]
    public void CleanJson_RemovesClosingMarkdownFence()
    {
        string raw = "{\"key\":\"value\"}\n```";
        string result = ApiService.CleanJson(raw);
        Assert.DoesNotContain("```", result);
    }

    [Fact]
    public void CleanJson_TrimsWhitespace()
    {
        string raw = "  {\"key\":\"value\"}  ";
        string result = ApiService.CleanJson(raw);
        Assert.Equal("{\"key\":\"value\"}", result);
    }

    [Fact]
    public void CleanJson_LeavesCleanJsonUntouched()
    {
        string raw = "{\"key\":\"value\"}";
        string result = ApiService.CleanJson(raw);
        Assert.Equal("{\"key\":\"value\"}", result);
    }

    // ──────────────────────────────────────────
    // BuildCardSystemPrompt
    // ──────────────────────────────────────────

    [Fact]
    public void BuildCardSystemPrompt_SingleCard_UsesSingularLanguage()
    {
        string prompt = ApiService.BuildCardSystemPrompt(1);
        Assert.Contains("1 recipe card", prompt);
        Assert.Contains("1 varied recipe", prompt);
    }

    [Fact]
    public void BuildCardSystemPrompt_MultipleCards_UsesPluralLanguage()
    {
        string prompt = ApiService.BuildCardSystemPrompt(3);
        Assert.Contains("3 recipe cards", prompt);
        Assert.Contains("3 varied and distinct recipes", prompt);
    }

    [Fact]
    public void BuildCardSystemPrompt_AlwaysContainsJsonStructure()
    {
        string prompt = ApiService.BuildCardSystemPrompt(2);
        Assert.Contains("RecipeName", prompt);
        Assert.Contains("Difficulty", prompt);
        Assert.Contains("MealType", prompt);
        Assert.Contains("CookTimeMinutes", prompt);
    }

    // ──────────────────────────────────────────
    // BuildUserPrompt
    // ──────────────────────────────────────────

    [Fact]
    public void BuildUserPrompt_AnyMealType_SaysAnyMealType()
    {
        var p = new RecipeParameterModel { MealType = "Any", Difficulty = "Easy" };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("any meal type", prompt);
    }

    [Fact]
    public void BuildUserPrompt_SpecificMealType_UsesIt()
    {
        var p = new RecipeParameterModel { MealType = "Dinner", Difficulty = "Any" };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Dinner", prompt);
    }

    [Fact]
    public void BuildUserPrompt_AnyDifficulty_SaysAnyDifficulty()
    {
        var p = new RecipeParameterModel { MealType = "Any", Difficulty = "Any" };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("any difficulty", prompt);
    }

    [Fact]
    public void BuildUserPrompt_SpecificDifficulty_UsesIt()
    {
        var p = new RecipeParameterModel { MealType = "Any", Difficulty = "Hard" };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Hard", prompt);
    }

    [Fact]
    public void BuildUserPrompt_AnyCookTime_SaysAnyDuration()
    {
        var p = new RecipeParameterModel { AnyCookTime = true };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("any duration", prompt);
    }

    [Fact]
    public void BuildUserPrompt_SpecificCookTime_AppliesBuffer()
    {
        // 30 min base → 20% buffer = 6 min, but min buffer is 15, so upper = 30 + 15 = 45
        var p = new RecipeParameterModel { AnyCookTime = false, CookTimeMinutes = 30 };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("under 45 minutes", prompt);
    }

    [Fact]
    public void BuildUserPrompt_LargeCookTime_Uses20PercentBuffer()
    {
        // 120 min → 20% = 24 min (> 15), so upper = 120 + 24 = 144
        var p = new RecipeParameterModel { AnyCookTime = false, CookTimeMinutes = 120 };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("under 144 minutes", prompt);
    }

    [Fact]
    public void BuildUserPrompt_OnlyIngredientsMode_UsesStrictInstruction()
    {
        var p = new RecipeParameterModel
        {
            IngredientUsage = "Only ingredients I have",
            Ingredients = ["Chicken", "Rice"]
        };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Only use ingredients from this list", prompt);
        Assert.Contains("Chicken", prompt);
        Assert.Contains("Rice", prompt);
    }

    [Fact]
    public void BuildUserPrompt_MainlyIngredientsMode_UsesPreferInstruction()
    {
        var p = new RecipeParameterModel
        {
            IngredientUsage = "Mainly ingredients I have",
            Ingredients = ["Eggs"]
        };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Prefer these ingredients", prompt);
    }

    [Fact]
    public void BuildUserPrompt_AnyIngredientsMode_UsesFreeInstruction()
    {
        var p = new RecipeParameterModel
        {
            IngredientUsage = "Any ingredients",
            Ingredients = ["Pasta"]
        };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Feel free to use any ingredients", prompt);
    }

    [Fact]
    public void BuildUserPrompt_NoIngredients_OmitsIngredientSection()
    {
        var p = new RecipeParameterModel { Ingredients = [] };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.DoesNotContain("ingredients available", prompt);
    }

    [Fact]
    public void BuildUserPrompt_WithAllergens_ListsThem()
    {
        var p = new RecipeParameterModel { Allergies = ["Gluten", "Nuts"] };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Gluten", prompt);
        Assert.Contains("Nuts", prompt);
        Assert.Contains("Must avoid", prompt);
    }

    [Fact]
    public void BuildUserPrompt_Vegetarian_IncludesFlag()
    {
        var p = new RecipeParameterModel { Vegetarian = true };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Must be vegetarian", prompt);
    }

    [Fact]
    public void BuildUserPrompt_Vegan_IncludesFlag()
    {
        var p = new RecipeParameterModel { Vegan = true };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Must be vegan", prompt);
    }

    [Fact]
    public void BuildUserPrompt_GlutenFree_IncludesFlag()
    {
        var p = new RecipeParameterModel { GlutenFree = true };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Must be gluten-free", prompt);
    }

    [Fact]
    public void BuildUserPrompt_DairyFree_IncludesFlag()
    {
        var p = new RecipeParameterModel { DairyFree = true };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Must be dairy-free", prompt);
    }

    [Fact]
    public void BuildUserPrompt_NoDietaryFlags_OmitsTheirPhrases()
    {
        var p = new RecipeParameterModel
        {
            Vegetarian = false, Vegan = false, GlutenFree = false, DairyFree = false
        };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.DoesNotContain("vegetarian", prompt);
        Assert.DoesNotContain("vegan", prompt);
        Assert.DoesNotContain("gluten-free", prompt);
        Assert.DoesNotContain("dairy-free", prompt);
    }

    [Fact]
    public void BuildUserPrompt_AdditionalNotes_IncludesThem()
    {
        var p = new RecipeParameterModel { AdditionalNotes = "Make it spicy" };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("Make it spicy", prompt);
    }

    [Fact]
    public void BuildUserPrompt_EmptyAdditionalNotes_OmitsSection()
    {
        var p = new RecipeParameterModel { AdditionalNotes = "" };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.DoesNotContain("Additional notes", prompt);
    }

    [Fact]
    public void BuildUserPrompt_ServingsIncludedInPrompt()
    {
        var p = new RecipeParameterModel { Servings = 6 };
        string prompt = ApiService.BuildUserPrompt(p);
        Assert.Contains("6 servings", prompt);
    }

    // ──────────────────────────────────────────
    // BuildFullRecipeUserPrompt
    // ──────────────────────────────────────────

    [Fact]
    public void BuildFullRecipeUserPrompt_ContainsRecipeName()
    {
        var card = new RecipeCardModel { RecipeName = "Spicy Thai Noodles", Difficulty = "Easy", MealType = "Dinner" };
        var p = new RecipeParameterModel();
        string prompt = ApiService.BuildFullRecipeUserPrompt(card, p);
        Assert.Contains("Spicy Thai Noodles", prompt);
    }

    [Fact]
    public void BuildFullRecipeUserPrompt_ContainsDifficultyAndMealType()
    {
        var card = new RecipeCardModel { RecipeName = "Test", Difficulty = "Hard", MealType = "Lunch" };
        var p = new RecipeParameterModel();
        string prompt = ApiService.BuildFullRecipeUserPrompt(card, p);
        Assert.Contains("Hard", prompt);
        Assert.Contains("Lunch", prompt);
    }

    [Fact]
    public void BuildFullRecipeUserPrompt_ContainsTargetCookTime()
    {
        var card = new RecipeCardModel { RecipeName = "Test", CookTimeMinutes = 45 };
        var p = new RecipeParameterModel();
        string prompt = ApiService.BuildFullRecipeUserPrompt(card, p);
        Assert.Contains("45 minutes", prompt);
    }

    [Fact]
    public void BuildFullRecipeUserPrompt_WithAllergens_ListsThem()
    {
        var card = new RecipeCardModel { RecipeName = "Test" };
        var p = new RecipeParameterModel { Allergies = ["Fish", "Soya"] };
        string prompt = ApiService.BuildFullRecipeUserPrompt(card, p);
        Assert.Contains("Fish", prompt);
        Assert.Contains("Soya", prompt);
    }

    [Fact]
    public void BuildFullRecipeUserPrompt_WithDietaryFlags_IncludesThem()
    {
        var card = new RecipeCardModel { RecipeName = "Test" };
        var p = new RecipeParameterModel { Vegan = true, GlutenFree = true };
        string prompt = ApiService.BuildFullRecipeUserPrompt(card, p);
        Assert.Contains("vegan", prompt);
        Assert.Contains("gluten-free", prompt);
    }

    // ──────────────────────────────────────────
    // BuildFullRecipeSystemPrompt
    // ──────────────────────────────────────────

    [Fact]
    public void BuildFullRecipeSystemPrompt_ContainsRequiredJsonFields()
    {
        string prompt = ApiService.BuildFullRecipeSystemPrompt();
        Assert.Contains("RecipeName", prompt);
        Assert.Contains("Ingredients", prompt);
        Assert.Contains("Instructions", prompt);
        Assert.Contains("NutritionPerServing", prompt);
        Assert.Contains("StorageAdvice", prompt);
    }
}
