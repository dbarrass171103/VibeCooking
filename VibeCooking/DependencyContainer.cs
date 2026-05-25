using VibeCooking.ViewModels;

namespace VibeCooking;

public static class DependencyContainer
{
	public static void AddDependencies(this IServiceCollection services)
	{
		//View Models
		services.AddTransient<HomeViewModel>();
	}
}