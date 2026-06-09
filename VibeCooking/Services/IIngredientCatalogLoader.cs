namespace VibeCooking.Services;

/// <summary>
/// Abstracts the loading of the ingredient catalog JSON.
/// The real implementation reads from the MAUI app bundle via FileSystem;
/// tests supply a fake implementation without any platform dependency.
/// </summary>
public interface IIngredientCatalogLoader
{
    Task<string> LoadCatalogJsonAsync();
}
