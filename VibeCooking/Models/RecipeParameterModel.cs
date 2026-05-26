namespace VibeCooking.Models;

public class RecipeParameterModel
{
	public List<string> Ingredients { get; set; } = new();
	public List<string> Allergies { get; set; } = new();
	public int DifficultyLevel { get; set; } = 1; //1 - 5?
	
	//TODO Other Stuff?
}