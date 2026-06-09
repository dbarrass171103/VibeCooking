using VibeCooking.Themes;

namespace VibeCooking.Services;

public interface IThemeService
{
    ThemePreset CurrentTheme { get; }
    bool IsDarkMode { get; }
    IReadOnlyList<ThemePreset> AvailableThemes { get; }

    /// Loads persisted preferences from storage. Call once from MainLayout.OnInitializedAsync.
    Task InitAsync();

    Task SetThemeAsync(ThemePreset theme);
    Task ToggleDarkModeAsync();

    /// Fired whenever the theme or dark-mode setting changes. MainLayout subscribes to re-render.
    event Action OnThemeChanged;
}
