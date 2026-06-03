using VibeCooking.Models;
using VibeCooking.Services;

namespace VibeCooking.ViewModels;

public class RecipeGenerationViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IngredientsViewModel _ingredientsViewModel;

    public List<string> MealTypes { get; } =
        ["Any", "Breakfast", "Lunch", "Dinner", "Snack", "Dessert"];

    public List<string> Difficulties { get; } =
        ["Any", "Easy", "Medium", "Hard"];

    public List<string> CuisineTypes { get; } =
    [
        "Any", "American", "Argentinian", "Australian", "Brazilian", "British",
        "Caribbean", "Chinese", "Danish", "Emirati", "Filipino", "Finnish",
        "French", "German", "Greek", "Hong Kong", "Indian", "Indonesian",
        "Italian", "Japanese", "Korean", "Lebanese", "Malaysian", "Mexican",
        "Moroccan", "Norwegian", "Pakistani", "Peruvian", "Saudi Arabian", "Singaporean",
        "Spanish", "Swedish", "Taiwanese", "Thai", "Turkish", "Vietnamese"
    ];

    public List<string> IngredientUsageOptions { get; } =
        ["Only ingredients I have", "Mainly ingredients I have", "Any ingredients"];

    public List<string> PresetAllergens { get; } =
    [
        "Gluten", "Crustaceans", "Eggs", "Fish", "Peanuts", "Soya", "Milk",
        "Nuts", "Celery", "Mustard", "Sesame", "Sulphites", "Lupin", "Molluscs"
    ];

    public RecipeParameterModel Parameters { get; private set; } = new();
    public List<string> CustomAllergens { get; private set; } = new();
    public List<RecipeCardModel> RecipeCards { get; private set; } = new();
    public RecipeCardModel? SelectedCard { get; private set; }
    public RecipeOutputModel? GeneratedRecipe { get; private set; }

    public bool IsGeneratingCards { get; private set; } = false;
    public bool IsGeneratingRecipe { get; private set; } = false;
    public string? ErrorMessage { get; private set; }

    public RecipeGenerationViewModel(IApiService apiService, IngredientsViewModel ingredientsViewModel)
    {
        _apiService = apiService;
        _ingredientsViewModel = ingredientsViewModel;
    }

    public override Task InitAsync() => Task.CompletedTask;

    /// <summary>
    /// Generates recipe cards. Pulls the current ingredient selection from
    /// IngredientsViewModel and merges preset and custom allergens into the parameter list.
    /// </summary>
    public async Task GenerateCardsAsync()
    {
        ErrorMessage = null;
        IsGeneratingCards = true;
        SelectedCard = null;
        GeneratedRecipe = null;
        RecipeCards = new();

        Parameters.Ingredients = _ingredientsViewModel.GetSelectedIngredients();
        Parameters.Allergies = GetAllAllergens();

        var (success, error, cards) = await _apiService.GenerateRecipeCardsAsync(Parameters);

        IsGeneratingCards = false;

        if (!success)
        {
            ErrorMessage = error;
            return;
        }

        RecipeCards = cards;
    }


    // Selects a recipe card and generates the full recipe for it.
    public async Task SelectCardAsync(RecipeCardModel card)
    {
        ErrorMessage = null;
        SelectedCard = card;
        IsGeneratingRecipe = true;

        var (success, error, recipe) = await _apiService.GenerateRecipeAsync(card, Parameters);

        IsGeneratingRecipe = false;

        if (!success)
        {
            ErrorMessage = error;
            SelectedCard = null;
            return;
        }

        GeneratedRecipe = recipe;
    }

    // Toggles an allergen on and off
    public void TogglePresetAllergen(string allergen)
    {
        if (Parameters.Allergies.Contains(allergen))
            Parameters.Allergies.Remove(allergen);
        else
            Parameters.Allergies.Add(allergen);
    }

    public bool IsAllergenSelected(string allergen) =>
        Parameters.Allergies.Contains(allergen, StringComparer.OrdinalIgnoreCase);

    // Adds a custom allergen. Ignores duplicates against both lists.
    public void AddCustomAllergen(string allergen)
    {
        allergen = allergen.Trim();

        if (string.IsNullOrWhiteSpace(allergen))
            return;

        bool alreadyExists =
            CustomAllergens.Any(x => x.Equals(allergen, StringComparison.OrdinalIgnoreCase)) ||
            PresetAllergens.Any(x => x.Equals(allergen, StringComparison.OrdinalIgnoreCase));

        if (!alreadyExists)
            CustomAllergens.Add(allergen);
    }

    public void RemoveCustomAllergen(string allergen) =>
        CustomAllergens.Remove(allergen);

    // Merges preset selections and custom allergens into one deduplicated list for the API.
    private List<string> GetAllAllergens() =>
        Parameters.Allergies
            .Concat(CustomAllergens)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}