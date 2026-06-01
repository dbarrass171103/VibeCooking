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

        // Services
        services.AddTransient<IApiService, ApiService>();
        services.AddSingleton<ILocalStorageService, LocalStorageService>();
    }
}