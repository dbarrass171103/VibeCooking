using System.Text.Json;
using VibeCooking.Models;
using VibeCooking.Services;

namespace VibeCooking.ViewModels;

public class IngredientsViewModel : BaseViewModel
{
    private readonly ILocalStorageService _storage;
    private readonly IIngredientCatalogLoader _catalogLoader;

    // Binary categories — ingredients in these groups are toggled on/off with no quantity.
    private static readonly HashSet<IngredientCategory> BinaryCategories =
    [
        IngredientCategory.Seasonings,
        IngredientCategory.OilsAndCondiments
    ];

    // All ingredients — catalog and custom — grouped by category.
    public Dictionary<IngredientCategory, List<UserIngredient>> IngredientsByCategory { get; private set; } = new();

    public IngredientsViewModel(ILocalStorageService storage, IIngredientCatalogLoader catalogLoader)
    {
        _storage = storage;
        _catalogLoader = catalogLoader;
    }

    // Loads the default catalog from the bundled JSON file then loads locally stored ingredients
    public override async Task InitAsync()
    {
        await LoadCatalogAsync();
        await _storage.LoadIngredientsAsync(this);
    }

    // Reads ingredients.json via the catalog loader and populates IngredientsByCategory.
    private async Task LoadCatalogAsync()
    {
        string json = await _catalogLoader.LoadCatalogJsonAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var ingredients = JsonSerializer.Deserialize<List<UserIngredient>>(json, options) ?? new();

        // Derive IsBinary from category and group for the UI.
        foreach (var ingredient in ingredients)
            ingredient.IsBinary = BinaryCategories.Contains(ingredient.Category);

        IngredientsByCategory = ingredients
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Ensure all categories exist in the dictionary even if the JSON has no entries for them.
        foreach (IngredientCategory category in Enum.GetValues<IngredientCategory>())
        {
            if (!IngredientsByCategory.ContainsKey(category))
                IngredientsByCategory[category] = new();
        }
    }

    // Adds a custom ingredient to the appropriate category section.
    public void AddCustomIngredient(string name, IngredientCategory category)
    {
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return;

        // Check for duplicates across all categories.
        bool alreadyExists = IngredientsByCategory.Values
            .SelectMany(x => x)
            .Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (!alreadyExists)
        {
            var ingredient = new UserIngredient
            {
                Name = name,
                Category = category,
                IsBinary = BinaryCategories.Contains(category),
                IsSelected = false,
                IsCustom = true
            };

            IngredientsByCategory[category].Add(ingredient);
            _ = _storage.SaveIngredientsAsync(this);
        }
    }

    public void RemoveCustomIngredient(UserIngredient ingredient)
    {
        if (IngredientsByCategory.TryGetValue(ingredient.Category, out var list))
            list.Remove(ingredient);

        _ = _storage.SaveIngredientsAsync(this);
    }

    // Selects a catalog ingredient, storing its quantity if provided
    public void SelectIngredient(UserIngredient ingredient, string? quantity = null)
    {
        ingredient.IsSelected = true;
        ingredient.Quantity = quantity?.Trim() ?? string.Empty;
        _ = _storage.SaveIngredientsAsync(this);
    }

    // Deselects a catalog ingredient and clears its quantity
    public void DeselectIngredient(UserIngredient ingredient)
    {
        ingredient.IsSelected = false;
        ingredient.Quantity = string.Empty;
        _ = _storage.SaveIngredientsAsync(this);
    }

    // Toggles a binary ingredient's selected state.
    public void ToggleBinary(UserIngredient ingredient)
    {
        ingredient.IsSelected = !ingredient.IsSelected;
        _ = _storage.SaveIngredientsAsync(this);
    }

    // Returns a flat list of all selected ingredient strings, ready to pass to the API.
    public List<string> GetSelectedIngredients()
    {
        return IngredientsByCategory.Values
            .SelectMany(x => x)
            .Where(x => x.IsSelected)
            .Select(i => string.IsNullOrWhiteSpace(i.Quantity)
                ? i.Name
                : $"{i.Name} ({i.Quantity})")
            .ToList();
    }
}