using QRCoder;

namespace VibeCooking.Services;

public class QrCodeService : IQrCodeService
{
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
}