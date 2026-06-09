namespace VibeCooking.Services;

/// <summary>
/// MAUI implementation of IIngredientCatalogLoader.
/// Reads ingredients.json from the app bundle using the MAUI FileSystem API.
/// </summary>
public class MauiIngredientCatalogLoader : IIngredientCatalogLoader
{
    public async Task<string> LoadCatalogJsonAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("ingredients.json");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
