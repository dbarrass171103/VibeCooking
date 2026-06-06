using VibeCooking.Themes;

namespace VibeCooking.Services;

public class ThemeService : IThemeService
{
    private const string ThemeKey = "app_theme_name";
    private const string DarkModeKey = "app_dark_mode";
    private const int PersistDays = 3650;

    private readonly IBarrel _barrel;

    public ThemePreset CurrentTheme { get; private set; } = AppThemes.Default;
    public bool IsDarkMode { get; private set; } = false;
    public IReadOnlyList<ThemePreset> AvailableThemes => AppThemes.All;

    public event Action? OnThemeChanged;

    public ThemeService(IBarrel barrel)
    {
        _barrel = barrel;
    }

    public Task InitAsync()
    {
        if (!_barrel.IsExpired(ThemeKey))
        {
            var name = _barrel.Get<string>(ThemeKey);
            if (name is not null)
                CurrentTheme = AppThemes.FindByName(name) ?? AppThemes.Default;
        }

        if (!_barrel.IsExpired(DarkModeKey))
            IsDarkMode = _barrel.Get<bool>(DarkModeKey);

        return Task.CompletedTask;
    }

    public Task SetThemeAsync(ThemePreset theme)
    {
        CurrentTheme = theme;
        _barrel.Add(ThemeKey, theme.Name, TimeSpan.FromDays(PersistDays));
        OnThemeChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task ToggleDarkModeAsync()
    {
        IsDarkMode = !IsDarkMode;
        _barrel.Add(DarkModeKey, IsDarkMode, TimeSpan.FromDays(PersistDays));
        OnThemeChanged?.Invoke();
        return Task.CompletedTask;
    }
}
