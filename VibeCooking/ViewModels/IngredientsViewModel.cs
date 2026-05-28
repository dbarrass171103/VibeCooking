using VibeCooking.Models;

namespace VibeCooking.ViewModels;

public class IngredientsViewModel : BaseViewModel
{
    // Catalog lists — all ingredients the user can pick from.
    // IsBinary = false: tapping opens a quantity dialog.
    // IsBinary = true: tapping toggles selected on/off with no quantity.

    public List<UserIngredient> Meat { get; } =
    [
        new() { Name = "Chicken" }, new() { Name = "Beef" }, new() { Name = "Pork" }, new() { Name = "Lamb" },
        new() { Name = "Turkey" }, new() { Name = "Bacon" }, new() { Name = "Sausage" }, new() { Name = "Salmon" },
        new() { Name = "Tuna" }, new() { Name = "Prawns" }, new() { Name = "Cod" }, new() { Name = "Mince" }
    ];

    public List<UserIngredient> Vegetables { get; } =
    [
        new() { Name = "Onion" }, new() { Name = "Garlic" }, new() { Name = "Tomato" }, new() { Name = "Potato" },
        new() { Name = "Carrot" }, new() { Name = "Broccoli" }, new() { Name = "Spinach" }, new() { Name = "Pepper" },
        new() { Name = "Mushroom" }, new() { Name = "Courgette" }, new() { Name = "Aubergine" }, new() { Name = "Leek" },
        new() { Name = "Celery" }, new() { Name = "Sweetcorn" }, new() { Name = "Peas" }, new() { Name = "Green Beans" }
    ];

    public List<UserIngredient> Fruit { get; } =
    [
        new() { Name = "Lemon" }, new() { Name = "Lime" }, new() { Name = "Apple" }, new() { Name = "Banana" },
        new() { Name = "Tomato" }, new() { Name = "Avocado" }, new() { Name = "Mango" }, new() { Name = "Pineapple" }
    ];

    public List<UserIngredient> Dairy { get; } =
    [
        new() { Name = "Milk" }, new() { Name = "Butter" }, new() { Name = "Cheddar" }, new() { Name = "Parmesan" },
        new() { Name = "Mozzarella" }, new() { Name = "Cream" }, new() { Name = "Yoghurt" }, new() { Name = "Eggs" },
        new() { Name = "Cream Cheese" }, new() { Name = "Sour Cream" }
    ];

    public List<UserIngredient> Grains { get; } =
    [
        new() { Name = "Rice" }, new() { Name = "Pasta" }, new() { Name = "Bread" }, new() { Name = "Noodles" },
        new() { Name = "Flour" }, new() { Name = "Oats" }, new() { Name = "Couscous" }, new() { Name = "Quinoa" }
    ];

    public List<UserIngredient> Seasonings { get; } =
    [
        new() { Name = "Salt", IsBinary = true },
        new() { Name = "Black Pepper", IsBinary = true },
        new() { Name = "Paprika", IsBinary = true },
        new() { Name = "Cumin", IsBinary = true },
        new() { Name = "Turmeric", IsBinary = true },
        new() { Name = "Chilli Flakes", IsBinary = true },
        new() { Name = "Oregano", IsBinary = true },
        new() { Name = "Basil", IsBinary = true },
        new() { Name = "Thyme", IsBinary = true },
        new() { Name = "Rosemary", IsBinary = true },
        new() { Name = "Coriander", IsBinary = true },
        new() { Name = "Cinnamon", IsBinary = true },
        new() { Name = "Garlic Powder", IsBinary = true },
        new() { Name = "Onion Powder", IsBinary = true },
        new() { Name = "Curry Powder", IsBinary = true },
        new() { Name = "Garam Masala", IsBinary = true }
    ];

    public List<UserIngredient> OilsAndCondiments { get; } =
    [
        new() { Name = "Olive Oil", IsBinary = true },
        new() { Name = "Vegetable Oil", IsBinary = true },
        new() { Name = "Sesame Oil", IsBinary = true },
        new() { Name = "Soy Sauce", IsBinary = true },
        new() { Name = "Worcestershire Sauce", IsBinary = true },
        new() { Name = "Hot Sauce", IsBinary = true },
        new() { Name = "Ketchup", IsBinary = true },
        new() { Name = "Mayonnaise", IsBinary = true },
        new() { Name = "Mustard", IsBinary = true },
        new() { Name = "Honey", IsBinary = true },
        new() { Name = "Vinegar", IsBinary = true },
        new() { Name = "Balsamic Vinegar", IsBinary = true },
        new() { Name = "Fish Sauce", IsBinary = true },
        new() { Name = "Oyster Sauce", IsBinary = true }
    ];

    /// <summary>
    /// Custom ingredients typed in by the user. IsCustom = true on all entries.
    /// </summary>
    public List<UserIngredient> CustomIngredients { get; set; } = new();

    // All catalog lists in one place — useful for save/load and bulk queries.
    private IEnumerable<UserIngredient> AllCatalogIngredients =>
        Meat.Concat(Vegetables).Concat(Fruit).Concat(Dairy)
            .Concat(Grains).Concat(Seasonings).Concat(OilsAndCondiments);

    public override Task InitAsync() => Task.CompletedTask;

    /// <summary>
    /// Selects a catalog ingredient, storing its quantity if provided.
    /// </summary>
    public void SelectIngredient(UserIngredient ingredient, string? quantity = null)
    {
        ingredient.IsSelected = true;
        ingredient.Quantity = quantity?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Deselects a catalog ingredient and clears its quantity.
    /// </summary>
    public void DeselectIngredient(UserIngredient ingredient)
    {
        ingredient.IsSelected = false;
        ingredient.Quantity = string.Empty;
    }

    /// <summary>
    /// Toggles a binary ingredient's selected state.
    /// </summary>
    public void ToggleBinary(UserIngredient ingredient)
    {
        ingredient.IsSelected = !ingredient.IsSelected;
    }

    /// <summary>
    /// Adds a custom ingredient entered by the user.
    /// Ignores duplicates (case-insensitive on name).
    /// </summary>
    public void AddCustomIngredient(string name, string? quantity = null)
    {
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return;

        bool alreadyExists = CustomIngredients
            .Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (!alreadyExists)
        {
            CustomIngredients.Add(new UserIngredient
            {
                Name = name,
                Quantity = quantity?.Trim() ?? string.Empty,
                IsSelected = true,
                IsCustom = true
            });
        }
    }

    public void RemoveCustomIngredient(UserIngredient ingredient)
    {
        CustomIngredients.Remove(ingredient);
    }

    /// <summary>
    /// Returns a flat list of all selected ingredient strings, ready to pass to the API.
    /// Includes quantity in brackets where provided.
    /// </summary>
    public List<string> GetSelectedIngredients()
    {
        var selected = AllCatalogIngredients
            .Where(x => x.IsSelected)
            .Concat(CustomIngredients);

        return selected.Select(i =>
            string.IsNullOrWhiteSpace(i.Quantity)
                ? i.Name
                : $"{i.Name} ({i.Quantity})")
            .ToList();
    }
}