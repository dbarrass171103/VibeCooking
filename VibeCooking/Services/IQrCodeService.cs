namespace VibeCooking.Services;

public interface IQrCodeService
{
	public (bool success, string errorMessage, byte[] qrcode) GenerateQrCodeSavedRecipe(string json);
}