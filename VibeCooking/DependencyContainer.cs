using MonkeyCache.FileStore;
using VibeCooking.Services;
using VibeCooking.ViewModels;

namespace VibeCooking;

public static class DependencyContainer
{
    public static void AddDependencies(this IServiceCollection services)
    {
        Barrel.ApplicationId = "VibeCooking";

        // View Models
        services.AddSingleton<IngredientsViewModel>();
        services.AddSingleton<RecipeGenerationViewModel>();
        services.AddSingleton<RecipeChatViewModel>();
        services.AddTransient<MyRecipesViewModel>();
        services.AddTransient<MealPrepViewModel>();

        // Services
        services.AddTransient<IApiService, ApiService>();
        services.AddSingleton<ILocalStorageService, LocalStorageService>();
        services.AddSingleton<IThemeService, ThemeService>();
    }
}