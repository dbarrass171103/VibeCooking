using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using VibeCooking.Models;

namespace VibeCooking.Services;

public class ApiService : IApiService
{
    private readonly ChatClient _chatClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(IConfiguration config)
    {
        string endpoint = config["AzureOpenAI:Endpoint"]!;
        string apiKey = config["AzureOpenAI:ApiKey"]!;
        string deploymentName = config["AzureOpenAI:DeploymentName"]!;

        AzureOpenAIClient azureClient = new(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey)
        );

        _chatClient = azureClient.GetChatClient(deploymentName);
    }

    // Generate Recipe Cards
    public async Task<(bool success, string errorMessage, List<RecipeCardModel> cards)> GenerateRecipeCardsAsync(RecipeParameterModel parameters)
    {
        try
        {
            string systemPrompt = BuildCardSystemPrompt();
            string userPrompt = BuildUserPrompt(parameters);

            ChatCompletion completion = await _chatClient.CompleteChatAsync(
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            );

            string rawJson = CleanJson(completion.Content[0].Text);

            List<RecipeCardModel>? cards = JsonSerializer.Deserialize<List<RecipeCardModel>>(rawJson, JsonOptions);

            if (cards is null || cards.Count == 0)
                return (false, "Failed to deserialise recipe cards.", new());

            return (true, string.Empty, cards);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, new());
        }
    }

    // Generate full recipe
    public async Task<(bool success, string errorMessage, RecipeOutputModel? output)> GenerateRecipeAsync(RecipeCardModel chosenCard, RecipeParameterModel parameters)
    {
        try
        {
            string systemPrompt = BuildFullRecipeSystemPrompt();
            string userPrompt = BuildFullRecipeUserPrompt(chosenCard, parameters);

            ChatCompletion completion = await _chatClient.CompleteChatAsync(
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            );

            string rawJson = CleanJson(completion.Content[0].Text);

            RecipeOutputModel? output = JsonSerializer.Deserialize<RecipeOutputModel>(rawJson, JsonOptions);

            if (output is null)
                return (false, "Failed to deserialise the full recipe.", null);

            return (true, string.Empty, output);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    private static string BuildCardSystemPrompt() => """
		You are a recipe generator. When asked, you return exactly 5 recipe cards
		as a valid JSON array and nothing else. no preamble, no explanation, no markdown fences.

		Each card in the array must exactly match this structure:
		{
		  "RecipeName": "string",
		  "Description": "string",
		  "CuisineType": "string",
		  "Difficulty": "Easy" | "Medium" | "Hard",
		  "MealType": "Breakfast" | "Lunch" | "Dinner" | "Snack" | "Dessert",
		  "CookTimeMinutes": int,
		  "PrepTimeMinutes": int,
		  "Servings": int,
		  "Ingredients": ["string"]
		}

		The Ingredients array should be a simple flat list of ingredient names only.
		Return exactly 5 varied and distinct recipes.
		""";

    private static string BuildFullRecipeSystemPrompt() => """
		You are a recipe generator. When asked, you return a single full recipe as a
		valid JSON object and nothing else. no preamble, no explanation, no markdown fences.

		The JSON must exactly match this structure:
		{
		  "RecipeName": "string",
		  "Description": "string",
		  "CuisineType": "string",
		  "Difficulty": "Easy" | "Medium" | "Hard",
		  "MealType": "Breakfast" | "Lunch" | "Dinner" | "Snack" | "Dessert",
		  "CookTimeMinutes": int,
		  "PrepTimeMinutes": int,
		  "Servings": int,
		  "Ingredients": [
		    {
		      "Name": "string",
		      "Quantity": "string",
		      "IsOptional": bool
		    }
		  ],
		  "Equipment": ["string"],
		  "Instructions": ["string"],
		  "NutritionPerServing": {
		    "CaloriesKcal": int,
		    "ProteinG": float,
		    "CarbsG": float,
		    "FatG": float,
		    "FibreG": float,
		    "SugarG": float,
		    "SaltG": float
		  },
		  "StorageAdvice": "string",
		  "Substitutions": ["string"]
		}
		""";

    private static string BuildUserPrompt(RecipeParameterModel p)
    {
        List<string> parts =
        [
            $"Generate 5 recipe cards for a {p.Difficulty} {p.MealType} for {p.Servings} servings.",
            $"Cuisine: {p.CuisineType}.",
            $"Target cook time: ~{p.CookTimeMinutes} minutes.",
        ];

        if (p.Ingredients.Count > 0)
            parts.Add($"Prioritise using these ingredients: {string.Join(", ", p.Ingredients)}.");

        if (p.Allergies.Count > 0)
            parts.Add($"Must avoid these allergens: {string.Join(", ", p.Allergies)}.");

        if (p.Vegetarian) parts.Add("Must be vegetarian.");
        if (p.Vegan) parts.Add("Must be vegan.");
        if (p.GlutenFree) parts.Add("Must be gluten-free.");
        if (p.DairyFree) parts.Add("Must be dairy-free.");

        if (!string.IsNullOrWhiteSpace(p.AdditionalNotes))
            parts.Add($"Additional notes: {p.AdditionalNotes}");

        return string.Join(" ", parts);
    }

    private static string BuildFullRecipeUserPrompt(RecipeCardModel card, RecipeParameterModel p)
    {
        List<string> parts =
        [
            $"Generate the full recipe for \"{card.RecipeName}\".",
            $"Description: {card.Description}",
            $"This is a {card.Difficulty} {card.MealType} recipe for {card.Servings} servings.",
            $"Target cook time: {card.CookTimeMinutes} minutes.",
        ];

        if (p.Allergies.Count > 0)
            parts.Add($"Must avoid these allergens: {string.Join(", ", p.Allergies)}.");

        if (p.Vegetarian) parts.Add("Must be vegetarian.");
        if (p.Vegan) parts.Add("Must be vegan.");
        if (p.GlutenFree) parts.Add("Must be gluten-free.");
        if (p.DairyFree) parts.Add("Must be dairy-free.");

        if (!string.IsNullOrWhiteSpace(p.AdditionalNotes))
            parts.Add($"Additional notes: {p.AdditionalNotes}");

        return string.Join(" ", parts);
    }

    private static string CleanJson(string raw) =>
        raw.Replace("```json", string.Empty)
           .Replace("```", string.Empty)
           .Trim();
}