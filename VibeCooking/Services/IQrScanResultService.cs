using VibeCooking.Models;

namespace VibeCooking.Services;

public interface IQrScanResultService
{
	public event Action<SavedRecipeModel>? OnRecipeScanned;
	public void Publish(SavedRecipeModel recipe);
}