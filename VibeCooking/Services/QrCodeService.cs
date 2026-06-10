using QRCoder;
using VibeCooking.Models;

namespace VibeCooking.Services;

public class QrCodeService : IQrCodeService, IQrScanResultService
{
	public event Action<SavedRecipeModel>? OnRecipeScanned;

	public (bool success, string errorMessage, byte[] qrcode) GenerateQrCodeSavedRecipe(string json)
	{
		try
		{
			byte[] qrcode = PngByteQRCodeHelper.GetQRCode(json, QRCodeGenerator.ECCLevel.L, 20);

			return (true, "", qrcode);
		}
		catch (Exception ex)
		{
			return (false, ex.Message, []);
		}
	}

	public void Publish(SavedRecipeModel recipe)
	{
		OnRecipeScanned?.Invoke(recipe);
	}
}