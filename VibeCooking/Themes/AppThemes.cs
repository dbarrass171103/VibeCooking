using MudBlazor;

namespace VibeCooking.Themes;

// A named visual theme, wraps a MudTheme with display metadata.
public record ThemePreset(string Name, string DisplayName, string SwatchColor, MudTheme MudTheme);

public static class AppThemes
{
    public static readonly ThemePreset Warm = new(
        Name: "warm",
        DisplayName: "Warm",
        SwatchColor: "#FF7043",
        MudTheme: new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#FF7043",
                PrimaryDarken = "#F4511E",
                PrimaryLighten = "#FFAB91",
                Secondary = "#FFA000",
                SecondaryDarken = "#FF8F00",
                SecondaryLighten = "#FFD54F",
                Tertiary = "#43A047",
                AppbarBackground = "#FFF8F3",
                AppbarText = "#4E342E",
                Background = "#FFFDF9",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFF8F3",
                DrawerText = "#5D4037",
                TextPrimary = "#3E2723",
                TextSecondary = "#6D4C41",
                Success = "#43A047",
                Warning = "#FB8C00",
                Error = "#E53935",
                Info = "#1E88E5",
                Divider = "#E0E0E0",
                LinesDefault = "#E0E0E0",
                TableLines = "#EEEEEE",
                TableStriped = "#FAFAFA"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#FF8A65",
                PrimaryDarken = "#FF7043",
                PrimaryLighten = "#FFAB91",
                Secondary = "#FFD54F",
                SecondaryDarken = "#FFC107",
                Tertiary = "#66BB6A",
                AppbarBackground = "#2A2A2A",
                AppbarText = "#F5F5F5",
                Background = "#1E1E1E",
                Surface = "#2A2A2A",
                DrawerBackground = "#252525",
                DrawerText = "#D7CCC8",
                TextPrimary = "#F5F5F5",
                TextSecondary = "#D7CCC8",
                Success = "#66BB6A",
                Warning = "#FFB74D",
                Error = "#EF5350",
                Info = "#42A5F5"
            },
            Typography = SharedTypography()
        });

    public static readonly ThemePreset Ocean = new(
        Name: "ocean",
        DisplayName: "Ocean",
        SwatchColor: "#0288D1",
        MudTheme: new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#0288D1",
                PrimaryDarken = "#01579B",
                PrimaryLighten = "#4FC3F7",
                Secondary = "#00897B",
                SecondaryDarken = "#00695C",
                SecondaryLighten = "#4DB6AC",
                Tertiary = "#7E57C2",
                AppbarBackground = "#E3F2FD",
                AppbarText = "#01579B",
                Background = "#F0F8FF",
                Surface = "#FFFFFF",
                DrawerBackground = "#E3F2FD",
                DrawerText = "#01579B",
                TextPrimary = "#01579B",
                TextSecondary = "#0277BD",
                Success = "#00897B",
                Warning = "#F57C00",
                Error = "#D32F2F",
                Info = "#0288D1",
                Divider = "#B3E5FC",
                LinesDefault = "#B3E5FC",
                TableLines = "#E1F5FE",
                TableStriped = "#F0F9FF"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#29B6F6",
                PrimaryDarken = "#0288D1",
                PrimaryLighten = "#81D4FA",
                Secondary = "#26A69A",
                SecondaryDarken = "#00897B",
                Tertiary = "#9575CD",
                AppbarBackground = "#0D1B2A",
                AppbarText = "#E3F2FD",
                Background = "#0D1B2A",
                Surface = "#1A2C3E",
                DrawerBackground = "#112233",
                DrawerText = "#B3E5FC",
                TextPrimary = "#E3F2FD",
                TextSecondary = "#B3E5FC",
                Success = "#26A69A",
                Warning = "#FFB74D",
                Error = "#EF5350",
                Info = "#29B6F6"
            },
            Typography = SharedTypography()
        });

    public static readonly ThemePreset Forest = new(
        Name: "forest",
        DisplayName: "Forest",
        SwatchColor: "#388E3C",
        MudTheme: new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#388E3C",
                PrimaryDarken = "#1B5E20",
                PrimaryLighten = "#81C784",
                Secondary = "#795548",
                SecondaryDarken = "#4E342E",
                SecondaryLighten = "#A1887F",
                Tertiary = "#FBC02D",
                AppbarBackground = "#F1F8E9",
                AppbarText = "#1B5E20",
                Background = "#F9FBF5",
                Surface = "#FFFFFF",
                DrawerBackground = "#F1F8E9",
                DrawerText = "#2E7D32",
                TextPrimary = "#1B5E20",
                TextSecondary = "#388E3C",
                Success = "#388E3C",
                Warning = "#F57F17",
                Error = "#C62828",
                Info = "#1565C0",
                Divider = "#DCEDC8",
                LinesDefault = "#DCEDC8",
                TableLines = "#F1F8E9",
                TableStriped = "#F9FBF5"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#66BB6A",
                PrimaryDarken = "#388E3C",
                PrimaryLighten = "#A5D6A7",
                Secondary = "#A1887F",
                SecondaryDarken = "#795548",
                Tertiary = "#FFD54F",
                AppbarBackground = "#1A2318",
                AppbarText = "#DCEDC8",
                Background = "#1A2318",
                Surface = "#263220",
                DrawerBackground = "#1F2A1D",
                DrawerText = "#C5E1A5",
                TextPrimary = "#F1F8E9",
                TextSecondary = "#C5E1A5",
                Success = "#66BB6A",
                Warning = "#FFB74D",
                Error = "#EF5350",
                Info = "#42A5F5"
            },
            Typography = SharedTypography()
        });

    public static readonly ThemePreset Monochrome = new(
        Name: "mono",
        DisplayName: "Mono",
        SwatchColor: "#424242",
        MudTheme: new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#424242",
                PrimaryDarken = "#212121",
                PrimaryLighten = "#757575",
                Secondary = "#757575",
                SecondaryDarken = "#616161",
                SecondaryLighten = "#9E9E9E",
                Tertiary = "#212121",
                AppbarBackground = "#FAFAFA",
                AppbarText = "#212121",
                Background = "#FFFFFF",
                Surface = "#F5F5F5",
                DrawerBackground = "#FAFAFA",
                DrawerText = "#424242",
                TextPrimary = "#212121",
                TextSecondary = "#616161",
                Success = "#388E3C",
                Warning = "#F57C00",
                Error = "#D32F2F",
                Info = "#1565C0",
                Divider = "#E0E0E0",
                LinesDefault = "#E0E0E0",
                TableLines = "#EEEEEE",
                TableStriped = "#FAFAFA"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#BDBDBD",
                PrimaryDarken = "#9E9E9E",
                PrimaryLighten = "#E0E0E0",
                Secondary = "#9E9E9E",
                SecondaryDarken = "#757575",
                Tertiary = "#E0E0E0",
                AppbarBackground = "#1C1C1C",
                AppbarText = "#F5F5F5",
                Background = "#121212",
                Surface = "#1E1E1E",
                DrawerBackground = "#181818",
                DrawerText = "#BDBDBD",
                TextPrimary = "#F5F5F5",
                TextSecondary = "#BDBDBD",
                Success = "#66BB6A",
                Warning = "#FFB74D",
                Error = "#EF5350",
                Info = "#42A5F5"
            },
            Typography = SharedTypography()
        });

    public static readonly IReadOnlyList<ThemePreset> All = [Warm, Ocean, Forest, Monochrome];
    public static ThemePreset Default => Warm;

    public static ThemePreset? FindByName(string name) =>
        All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static Typography SharedTypography() => new()
    {
        Default = new DefaultTypography
        {
            FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"]
        },
        H1 = new H1Typography { FontWeight = "700" },
        H2 = new H2Typography { FontWeight = "700" }
    };
}
