
using Heron.MudCalendar;
using Heron.MudTotalCalendar;
using VibeCooking.Models;

namespace VibeCooking.ViewModels;

public class MealPrepViewModel : BaseViewModel
{
	public List<CalendarItem> CalendarItems { get; set; } = new();
	public List<Value> CalendarTotalItems { get; set; } = new();

	public override async Task InitAsync()
	{
		CalendarItems = new()
		{
			new CalendarItem()
			{
				Text = "Chicken Broth",
				Start = DateTime.Now
			},

			new CalendarItem()
			{
				Text = "Tomato Soup",
				Start = DateTime.Today.AddDays(1)
			}
		};

		CalendarTotalItems = new()
		{
			new() { Date = DateTime.Today, Definition = new ValueDefinition() { Name = "chicken" }}
		};
	}


}