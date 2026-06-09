using System.IO.Compression;
using System.Text;

namespace VibeCooking.Utilities;

public class CompressionUtility
{
	public static string CompressToBase64(string text)
	{
		byte[] inputBytes = Encoding.UTF8.GetBytes(text);

		using var output = new MemoryStream();

		using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
		{
			brotli.Write(inputBytes, 0, inputBytes.Length);
		}

		return Convert.ToBase64String(output.ToArray());
	}

	public static string DecompressFromBase64(string compressedBase64)
	{
		byte[] compressedBytes = Convert.FromBase64String(compressedBase64);

		using var input = new MemoryStream(compressedBytes);
		using var brotli = new BrotliStream(input, CompressionMode.Decompress);
		using var reader = new StreamReader(brotli, Encoding.UTF8);

		return reader.ReadToEnd();
	}
}