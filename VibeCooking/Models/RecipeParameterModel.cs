namespace VibeCooking.Models;

public enum Difficulty
{
	Easy,
	Medium,
	Hard
}

public enum MealType
{
	Breakfast,
	Lunch,
	Dinner,
	Snack,
	Dessert
}

public enum CuisineType
{
    Any,
    American,
    Argentinian,
    Australian,
    Brazilian,
    British,
    Caribbean,
    Chinese,
    Danish,
    Emirati,
    Filipino,
    Finnish,
    French,
    German,
    Greek,
    HongKong,
    Indian,
    Indonesian,
    Italian,
    Japanese,
    Korean,
    Lebanese,
    Malaysian,
    Mexican,
    Moroccan,
    Norwegian,
    Peruvian,
    SaudiArabian,
    Singaporean,
    Spanish,
    Swedish,
    Taiwanese,
    Thai,
    Turkish,
    Vietnamese
}

// Holds user selected parameters. Passes to the API to build prompt to be sent to AI
public class RecipeParameterModel
{
	public List<string> Ingredients { get; set; } = new();
	public List<string> Allergies { get; set; } = new();
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;
    public MealType MealType { get; set; } = MealType.Dinner;
    public CuisineType CuisineType { get; set; } = CuisineType.Any;
    public int CookTimeMinutes { get; set; } = 30;
    public int Servings {  get; set; } = 4;
    public bool Vegetarian { get; set; } = false;
    public bool Vegan { get; set; } = false;
    public bool GlutenFree { get; set; } = false;
    public bool DairyFree { get; set; } = false;
    public string AdditionalNotes { get; set; } = string.Empty;

}