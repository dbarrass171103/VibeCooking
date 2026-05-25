namespace VibeCooking.Models;

public class SavedRecipeModel
{
	public string RecipeName { get; set; } = string.Empty;
	public string ImageBase64 { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsLiked { get; set; } = false;
}