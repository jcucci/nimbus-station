namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Provides preset theme configurations for popular color schemes.
/// </summary>
public static class ThemePresets
{
    // ========== Catppuccin Themes ==========

    /// <summary>Catppuccin Mocha - dark, warm flavor.</summary>
    public static ThemeConfig CatppuccinMocha { get; } = new(
        PromptColor: "#a6e3a1",        // Green
        PromptSessionColor: "#89dceb", // Sky
        PromptContextColor: "#f9e2af", // Yellow
        PromptCosmosAliasColor: "#fab387", // Peach
        PromptBlobAliasColor: "#cba6f7", // Mauve
        TableHeaderColor: "#89b4fa",   // Blue
        TableBorderColor: "#6c7086",   // Overlay0
        ErrorColor: "#f38ba8",         // Red
        WarningColor: "#f9e2af",       // Yellow
        SuccessColor: "#a6e3a1",       // Green
        DimColor: "#6c7086",           // Overlay0
        JsonKeyColor: "#89b4fa",       // Blue
        JsonStringColor: "#a6e3a1",    // Green
        JsonNumberColor: "#fab387",    // Peach
        JsonBooleanColor: "#f9e2af",   // Yellow
        JsonNullColor: "#6c7086",      // Overlay0
        BannerColor: "#89dceb");       // Sky

    /// <summary>Catppuccin Macchiato - dark, slightly lighter.</summary>
    public static ThemeConfig CatppuccinMacchiato { get; } = new(
        PromptColor: "#a6da95",        // Green
        PromptSessionColor: "#91d7e3", // Sky
        PromptContextColor: "#eed49f", // Yellow
        PromptCosmosAliasColor: "#f5a97f", // Peach
        PromptBlobAliasColor: "#c6a0f6", // Mauve
        TableHeaderColor: "#8aadf4",   // Blue
        TableBorderColor: "#6e738d",   // Overlay0
        ErrorColor: "#ed8796",         // Red
        WarningColor: "#eed49f",       // Yellow
        SuccessColor: "#a6da95",       // Green
        DimColor: "#6e738d",           // Overlay0
        JsonKeyColor: "#8aadf4",       // Blue
        JsonStringColor: "#a6da95",    // Green
        JsonNumberColor: "#f5a97f",    // Peach
        JsonBooleanColor: "#eed49f",   // Yellow
        JsonNullColor: "#6e738d",      // Overlay0
        BannerColor: "#91d7e3");       // Sky

    /// <summary>Catppuccin Frappé - medium contrast.</summary>
    public static ThemeConfig CatppuccinFrappe { get; } = new(
        PromptColor: "#a6d189",        // Green
        PromptSessionColor: "#99d1db", // Sky
        PromptContextColor: "#e5c890", // Yellow
        PromptCosmosAliasColor: "#ef9f76", // Peach
        PromptBlobAliasColor: "#ca9ee6", // Mauve
        TableHeaderColor: "#8caaee",   // Blue
        TableBorderColor: "#737994",   // Overlay0
        ErrorColor: "#e78284",         // Red
        WarningColor: "#e5c890",       // Yellow
        SuccessColor: "#a6d189",       // Green
        DimColor: "#737994",           // Overlay0
        JsonKeyColor: "#8caaee",       // Blue
        JsonStringColor: "#a6d189",    // Green
        JsonNumberColor: "#ef9f76",    // Peach
        JsonBooleanColor: "#e5c890",   // Yellow
        JsonNullColor: "#737994",      // Overlay0
        BannerColor: "#99d1db");       // Sky

    /// <summary>Catppuccin Latte - light theme.</summary>
    public static ThemeConfig CatppuccinLatte { get; } = new(
        PromptColor: "#40a02b",        // Green
        PromptSessionColor: "#04a5e5", // Sky
        PromptContextColor: "#df8e1d", // Yellow
        PromptCosmosAliasColor: "#fe640b", // Peach
        PromptBlobAliasColor: "#8839ef", // Mauve
        TableHeaderColor: "#1e66f5",   // Blue
        TableBorderColor: "#9ca0b0",   // Overlay0
        ErrorColor: "#d20f39",         // Red
        WarningColor: "#df8e1d",       // Yellow
        SuccessColor: "#40a02b",       // Green
        DimColor: "#9ca0b0",           // Overlay0
        JsonKeyColor: "#1e66f5",       // Blue
        JsonStringColor: "#40a02b",    // Green
        JsonNumberColor: "#fe640b",    // Peach
        JsonBooleanColor: "#df8e1d",   // Yellow
        JsonNullColor: "#9ca0b0",      // Overlay0
        BannerColor: "#04a5e5");       // Sky

    // ========== Dracula ==========

    /// <summary>Dracula - dark theme with vibrant colors.</summary>
    public static ThemeConfig Dracula { get; } = new(
        PromptColor: "#50fa7b",        // Green
        PromptSessionColor: "#8be9fd", // Cyan
        PromptContextColor: "#f1fa8c", // Yellow
        PromptCosmosAliasColor: "#ffb86c", // Orange
        PromptBlobAliasColor: "#bd93f9", // Purple
        TableHeaderColor: "#8be9fd",   // Cyan
        TableBorderColor: "#6272a4",   // Comment
        ErrorColor: "#ff5555",         // Red
        WarningColor: "#f1fa8c",       // Yellow
        SuccessColor: "#50fa7b",       // Green
        DimColor: "#6272a4",           // Comment
        JsonKeyColor: "#8be9fd",       // Cyan
        JsonStringColor: "#f1fa8c",    // Yellow
        JsonNumberColor: "#bd93f9",    // Purple
        JsonBooleanColor: "#ff79c6",   // Pink
        JsonNullColor: "#6272a4",      // Comment
        BannerColor: "#bd93f9");       // Purple

    // ========== One Dark ==========

    /// <summary>One Dark - Atom's iconic dark theme.</summary>
    public static ThemeConfig OneDark { get; } = new(
        PromptColor: "#98c379",        // Green
        PromptSessionColor: "#56b6c2", // Cyan
        PromptContextColor: "#e5c07b", // Yellow
        PromptCosmosAliasColor: "#d19a66", // Orange
        PromptBlobAliasColor: "#c678dd", // Purple
        TableHeaderColor: "#61afef",   // Blue
        TableBorderColor: "#5c6370",   // Comment
        ErrorColor: "#e06c75",         // Red
        WarningColor: "#e5c07b",       // Yellow
        SuccessColor: "#98c379",       // Green
        DimColor: "#5c6370",           // Comment
        JsonKeyColor: "#61afef",       // Blue
        JsonStringColor: "#98c379",    // Green
        JsonNumberColor: "#d19a66",    // Orange
        JsonBooleanColor: "#e5c07b",   // Yellow
        JsonNullColor: "#5c6370",      // Comment
        BannerColor: "#61afef");       // Blue

    // ========== Gruvbox ==========

    /// <summary>Gruvbox Dark - retro groove dark theme.</summary>
    public static ThemeConfig GruvboxDark { get; } = new(
        PromptColor: "#b8bb26",        // Green
        PromptSessionColor: "#83a598", // Aqua
        PromptContextColor: "#fabd2f", // Yellow
        PromptCosmosAliasColor: "#fe8019", // Orange
        PromptBlobAliasColor: "#d3869b", // Purple
        TableHeaderColor: "#83a598",   // Aqua
        TableBorderColor: "#504945",   // Dark gray
        ErrorColor: "#fb4934",         // Red
        WarningColor: "#fabd2f",       // Yellow
        SuccessColor: "#b8bb26",       // Green
        DimColor: "#928374",           // Gray
        JsonKeyColor: "#83a598",       // Aqua
        JsonStringColor: "#b8bb26",    // Green
        JsonNumberColor: "#d3869b",    // Purple
        JsonBooleanColor: "#fabd2f",   // Yellow
        JsonNullColor: "#928374",      // Gray
        BannerColor: "#83a598");       // Aqua

    /// <summary>Gruvbox Light - retro groove light theme.</summary>
    public static ThemeConfig GruvboxLight { get; } = new(
        PromptColor: "#79740e",        // Green
        PromptSessionColor: "#427b58", // Aqua
        PromptContextColor: "#b57614", // Yellow
        PromptCosmosAliasColor: "#af3a03", // Orange
        PromptBlobAliasColor: "#8f3f71", // Purple
        TableHeaderColor: "#427b58",   // Aqua
        TableBorderColor: "#d5c4a1",   // Light gray
        ErrorColor: "#9d0006",         // Red
        WarningColor: "#b57614",       // Yellow
        SuccessColor: "#79740e",       // Green
        DimColor: "#928374",           // Gray
        JsonKeyColor: "#427b58",       // Aqua
        JsonStringColor: "#79740e",    // Green
        JsonNumberColor: "#8f3f71",    // Purple
        JsonBooleanColor: "#b57614",   // Yellow
        JsonNullColor: "#928374",      // Gray
        BannerColor: "#427b58");       // Aqua

    // ========== Ayu ==========

    /// <summary>Ayu Dark - elegant dark theme.</summary>
    public static ThemeConfig AyuDark { get; } = new(
        PromptColor: "#aad94c",        // Green
        PromptSessionColor: "#73b8ff", // Blue
        PromptContextColor: "#ffb454", // Yellow
        PromptCosmosAliasColor: "#ff8f40", // Orange
        PromptBlobAliasColor: "#d2a6ff", // Purple
        TableHeaderColor: "#73b8ff",   // Blue
        TableBorderColor: "#565b66",   // Gray
        ErrorColor: "#f07178",         // Red
        WarningColor: "#ffb454",       // Yellow
        SuccessColor: "#aad94c",       // Green
        DimColor: "#565b66",           // Gray
        JsonKeyColor: "#73b8ff",       // Blue
        JsonStringColor: "#aad94c",    // Green
        JsonNumberColor: "#d2a6ff",    // Purple
        JsonBooleanColor: "#ffb454",   // Yellow
        JsonNullColor: "#565b66",      // Gray
        BannerColor: "#73b8ff");       // Blue

    /// <summary>Ayu Mirage - dark with warmer tones.</summary>
    public static ThemeConfig AyuMirage { get; } = new(
        PromptColor: "#d5ff80",        // Green
        PromptSessionColor: "#73d0ff", // Blue
        PromptContextColor: "#ffd173", // Yellow
        PromptCosmosAliasColor: "#ffad66", // Orange
        PromptBlobAliasColor: "#dfbfff", // Purple
        TableHeaderColor: "#73d0ff",   // Blue
        TableBorderColor: "#5c6773",   // Gray
        ErrorColor: "#ff6666",         // Red
        WarningColor: "#ffd173",       // Yellow
        SuccessColor: "#d5ff80",       // Green
        DimColor: "#5c6773",           // Gray
        JsonKeyColor: "#73d0ff",       // Blue
        JsonStringColor: "#d5ff80",    // Green
        JsonNumberColor: "#dfbfff",    // Purple
        JsonBooleanColor: "#ffd173",   // Yellow
        JsonNullColor: "#5c6773",      // Gray
        BannerColor: "#73d0ff");       // Blue

    /// <summary>Ayu Light - elegant light theme.</summary>
    public static ThemeConfig AyuLight { get; } = new(
        PromptColor: "#86b300",        // Green
        PromptSessionColor: "#399ee6", // Blue
        PromptContextColor: "#f2ae49", // Yellow
        PromptCosmosAliasColor: "#fa8d3e", // Orange
        PromptBlobAliasColor: "#a37acc", // Purple
        TableHeaderColor: "#399ee6",   // Blue
        TableBorderColor: "#8a9199",   // Gray
        ErrorColor: "#f51818",         // Red
        WarningColor: "#f2ae49",       // Yellow
        SuccessColor: "#86b300",       // Green
        DimColor: "#8a9199",           // Gray
        JsonKeyColor: "#399ee6",       // Blue
        JsonStringColor: "#86b300",    // Green
        JsonNumberColor: "#a37acc",    // Purple
        JsonBooleanColor: "#f2ae49",   // Yellow
        JsonNullColor: "#8a9199",      // Gray
        BannerColor: "#399ee6");       // Blue

    // ========== GitHub ==========

    /// <summary>GitHub Dark - GitHub's dark mode.</summary>
    public static ThemeConfig GitHubDark { get; } = new(
        PromptColor: "#3fb950",        // Green
        PromptSessionColor: "#58a6ff", // Blue
        PromptContextColor: "#d29922", // Yellow
        PromptCosmosAliasColor: "#db6d28", // Orange
        PromptBlobAliasColor: "#a371f7", // Purple
        TableHeaderColor: "#58a6ff",   // Blue
        TableBorderColor: "#484f58",   // Gray
        ErrorColor: "#f85149",         // Red
        WarningColor: "#d29922",       // Yellow
        SuccessColor: "#3fb950",       // Green
        DimColor: "#484f58",           // Gray
        JsonKeyColor: "#58a6ff",       // Blue
        JsonStringColor: "#a5d6ff",    // Light blue
        JsonNumberColor: "#a371f7",    // Purple
        JsonBooleanColor: "#d29922",   // Yellow
        JsonNullColor: "#484f58",      // Gray
        BannerColor: "#58a6ff");       // Blue

    /// <summary>GitHub Light - GitHub's light mode.</summary>
    public static ThemeConfig GitHubLight { get; } = new(
        PromptColor: "#1a7f37",        // Green
        PromptSessionColor: "#0969da", // Blue
        PromptContextColor: "#9a6700", // Yellow
        PromptCosmosAliasColor: "#bc4c00", // Orange
        PromptBlobAliasColor: "#8250df", // Purple
        TableHeaderColor: "#0969da",   // Blue
        TableBorderColor: "#d0d7de",   // Gray
        ErrorColor: "#cf222e",         // Red
        WarningColor: "#9a6700",       // Yellow
        SuccessColor: "#1a7f37",       // Green
        DimColor: "#6e7781",           // Gray
        JsonKeyColor: "#0969da",       // Blue
        JsonStringColor: "#0a3069",    // Dark blue
        JsonNumberColor: "#8250df",    // Purple
        JsonBooleanColor: "#9a6700",   // Yellow
        JsonNullColor: "#6e7781",      // Gray
        BannerColor: "#0969da");       // Blue

    // ========== Xcode ==========

    /// <summary>Xcode Dark - Apple's dark theme.</summary>
    public static ThemeConfig XcodeDark { get; } = new(
        PromptColor: "#84dc7c",        // Green
        PromptSessionColor: "#6bdfff", // Cyan
        PromptContextColor: "#fef383", // Yellow
        PromptCosmosAliasColor: "#ffa14f", // Orange
        PromptBlobAliasColor: "#d9a6ff", // Purple
        TableHeaderColor: "#4eb0cc",   // Blue
        TableBorderColor: "#6c7986",   // Gray
        ErrorColor: "#ff8170",         // Red
        WarningColor: "#fef383",       // Yellow
        SuccessColor: "#84dc7c",       // Green
        DimColor: "#6c7986",           // Gray
        JsonKeyColor: "#6bdfff",       // Cyan
        JsonStringColor: "#ff8170",    // Salmon
        JsonNumberColor: "#d9c97c",    // Light yellow
        JsonBooleanColor: "#fef383",   // Yellow
        JsonNullColor: "#6c7986",      // Gray
        BannerColor: "#6bdfff");       // Cyan

    /// <summary>Xcode Light - Apple's light theme.</summary>
    public static ThemeConfig XcodeLight { get; } = new(
        PromptColor: "#1d7d41",        // Green
        PromptSessionColor: "#3e8087", // Teal
        PromptContextColor: "#78492a", // Brown
        PromptCosmosAliasColor: "#c33720", // Orange
        PromptBlobAliasColor: "#ad3da4", // Purple
        TableHeaderColor: "#0f68a0",   // Blue
        TableBorderColor: "#c5c5c5",   // Gray
        ErrorColor: "#c33720",         // Red
        WarningColor: "#78492a",       // Brown
        SuccessColor: "#1d7d41",       // Green
        DimColor: "#8e8e93",           // Gray
        JsonKeyColor: "#3e8087",       // Teal
        JsonStringColor: "#c33720",    // Red
        JsonNumberColor: "#1c00cf",    // Blue
        JsonBooleanColor: "#ad3da4",   // Purple
        JsonNullColor: "#8e8e93",      // Gray
        BannerColor: "#0f68a0");       // Blue

    // ========== TokyoNight ==========

    /// <summary>TokyoNight Night - dark blue theme.</summary>
    public static ThemeConfig TokyoNightNight { get; } = new(
        PromptColor: "#9ece6a",        // Green
        PromptSessionColor: "#7dcfff", // Cyan
        PromptContextColor: "#e0af68", // Yellow
        PromptCosmosAliasColor: "#ff9e64", // Orange
        PromptBlobAliasColor: "#bb9af7", // Purple
        TableHeaderColor: "#7aa2f7",   // Blue
        TableBorderColor: "#565f89",   // Gray
        ErrorColor: "#f7768e",         // Red
        WarningColor: "#e0af68",       // Yellow
        SuccessColor: "#9ece6a",       // Green
        DimColor: "#565f89",           // Gray
        JsonKeyColor: "#7aa2f7",       // Blue
        JsonStringColor: "#9ece6a",    // Green
        JsonNumberColor: "#ff9e64",    // Orange
        JsonBooleanColor: "#e0af68",   // Yellow
        JsonNullColor: "#565f89",      // Gray
        BannerColor: "#7dcfff");       // Cyan

    /// <summary>TokyoNight Storm - dark with lighter background.</summary>
    public static ThemeConfig TokyoNightStorm { get; } = new(
        PromptColor: "#9ece6a",        // Green
        PromptSessionColor: "#7dcfff", // Cyan
        PromptContextColor: "#e0af68", // Yellow
        PromptCosmosAliasColor: "#ff9e64", // Orange
        PromptBlobAliasColor: "#bb9af7", // Purple
        TableHeaderColor: "#7aa2f7",   // Blue
        TableBorderColor: "#545c7e",   // Gray
        ErrorColor: "#f7768e",         // Red
        WarningColor: "#e0af68",       // Yellow
        SuccessColor: "#9ece6a",       // Green
        DimColor: "#545c7e",           // Gray
        JsonKeyColor: "#7aa2f7",       // Blue
        JsonStringColor: "#9ece6a",    // Green
        JsonNumberColor: "#ff9e64",    // Orange
        JsonBooleanColor: "#e0af68",   // Yellow
        JsonNullColor: "#545c7e",      // Gray
        BannerColor: "#7dcfff");       // Cyan

    /// <summary>TokyoNight Day - light variant.</summary>
    public static ThemeConfig TokyoNightDay { get; } = new(
        PromptColor: "#587539",        // Green
        PromptSessionColor: "#007197", // Cyan
        PromptContextColor: "#8c6c3e", // Yellow
        PromptCosmosAliasColor: "#965027", // Orange
        PromptBlobAliasColor: "#7847bd", // Purple
        TableHeaderColor: "#2e7de9",   // Blue
        TableBorderColor: "#9699a3",   // Gray
        ErrorColor: "#f52a65",         // Red
        WarningColor: "#8c6c3e",       // Yellow
        SuccessColor: "#587539",       // Green
        DimColor: "#9699a3",           // Gray
        JsonKeyColor: "#2e7de9",       // Blue
        JsonStringColor: "#587539",    // Green
        JsonNumberColor: "#965027",    // Orange
        JsonBooleanColor: "#8c6c3e",   // Yellow
        JsonNullColor: "#9699a3",      // Gray
        BannerColor: "#007197");       // Cyan

    // ========== Nord ==========

    /// <summary>Nord - arctic, north-bluish theme.</summary>
    public static ThemeConfig Nord { get; } = new(
        PromptColor: "#a3be8c",        // nord14 - Green
        PromptSessionColor: "#88c0d0", // nord8 - Frost cyan
        PromptContextColor: "#ebcb8b", // nord13 - Yellow
        PromptCosmosAliasColor: "#d08770", // nord12 - Orange
        PromptBlobAliasColor: "#b48ead", // nord15 - Purple
        TableHeaderColor: "#81a1c1",   // nord9 - Frost blue
        TableBorderColor: "#4c566a",   // nord3 - Dark gray
        ErrorColor: "#bf616a",         // nord11 - Red
        WarningColor: "#ebcb8b",       // nord13 - Yellow
        SuccessColor: "#a3be8c",       // nord14 - Green
        DimColor: "#4c566a",           // nord3
        JsonKeyColor: "#81a1c1",       // nord9
        JsonStringColor: "#a3be8c",    // nord14
        JsonNumberColor: "#b48ead",    // nord15
        JsonBooleanColor: "#ebcb8b",   // nord13
        JsonNullColor: "#4c566a",      // nord3
        BannerColor: "#88c0d0");       // nord8

    // ========== VSCode ==========

    /// <summary>VSCode Dark+ - Visual Studio Code's default dark theme.</summary>
    public static ThemeConfig VSCodeDark { get; } = new(
        PromptColor: "#6a9955",        // Green
        PromptSessionColor: "#4ec9b0", // Teal
        PromptContextColor: "#dcdcaa", // Yellow
        PromptCosmosAliasColor: "#ce9178", // Orange
        PromptBlobAliasColor: "#c586c0", // Purple
        TableHeaderColor: "#569cd6",   // Blue
        TableBorderColor: "#6e7681",   // Gray
        ErrorColor: "#f14c4c",         // Red
        WarningColor: "#dcdcaa",       // Yellow
        SuccessColor: "#6a9955",       // Green
        DimColor: "#6e7681",           // Gray
        JsonKeyColor: "#9cdcfe",       // Light blue
        JsonStringColor: "#ce9178",    // Orange
        JsonNumberColor: "#b5cea8",    // Light green
        JsonBooleanColor: "#569cd6",   // Blue
        JsonNullColor: "#569cd6",      // Blue
        BannerColor: "#4ec9b0");       // Teal

    /// <summary>VSCode Light+ - Visual Studio Code's default light theme.</summary>
    public static ThemeConfig VSCodeLight { get; } = new(
        PromptColor: "#008000",        // Green
        PromptSessionColor: "#267f99", // Teal
        PromptContextColor: "#795e26", // Yellow
        PromptCosmosAliasColor: "#a31515", // Red
        PromptBlobAliasColor: "#af00db", // Purple
        TableHeaderColor: "#0000ff",   // Blue
        TableBorderColor: "#c5c5c5",   // Gray
        ErrorColor: "#cd3131",         // Red
        WarningColor: "#795e26",       // Yellow
        SuccessColor: "#008000",       // Green
        DimColor: "#6e7681",           // Gray
        JsonKeyColor: "#0451a5",       // Blue
        JsonStringColor: "#a31515",    // Red
        JsonNumberColor: "#098658",    // Green
        JsonBooleanColor: "#0000ff",   // Blue
        JsonNullColor: "#0000ff",      // Blue
        BannerColor: "#267f99");       // Teal

    // ========== Material ==========

    /// <summary>Material Darker - Material Design dark theme.</summary>
    public static ThemeConfig MaterialDarker { get; } = new(
        PromptColor: "#c3e88d",        // Green
        PromptSessionColor: "#89ddff", // Cyan
        PromptContextColor: "#ffcb6b", // Yellow
        PromptCosmosAliasColor: "#f78c6c", // Orange
        PromptBlobAliasColor: "#c792ea", // Purple
        TableHeaderColor: "#82aaff",   // Blue
        TableBorderColor: "#545454",   // Gray
        ErrorColor: "#f07178",         // Red
        WarningColor: "#ffcb6b",       // Yellow
        SuccessColor: "#c3e88d",       // Green
        DimColor: "#545454",           // Gray
        JsonKeyColor: "#82aaff",       // Blue
        JsonStringColor: "#c3e88d",    // Green
        JsonNumberColor: "#f78c6c",    // Orange
        JsonBooleanColor: "#ffcb6b",   // Yellow
        JsonNullColor: "#545454",      // Gray
        BannerColor: "#89ddff");       // Cyan

    /// <summary>Material Ocean - Material Design ocean variant.</summary>
    public static ThemeConfig MaterialOcean { get; } = new(
        PromptColor: "#c3e88d",        // Green
        PromptSessionColor: "#89ddff", // Cyan
        PromptContextColor: "#ffcb6b", // Yellow
        PromptCosmosAliasColor: "#f78c6c", // Orange
        PromptBlobAliasColor: "#c792ea", // Purple
        TableHeaderColor: "#82aaff",   // Blue
        TableBorderColor: "#464b5d",   // Gray
        ErrorColor: "#f07178",         // Red
        WarningColor: "#ffcb6b",       // Yellow
        SuccessColor: "#c3e88d",       // Green
        DimColor: "#464b5d",           // Gray
        JsonKeyColor: "#82aaff",       // Blue
        JsonStringColor: "#c3e88d",    // Green
        JsonNumberColor: "#f78c6c",    // Orange
        JsonBooleanColor: "#ffcb6b",   // Yellow
        JsonNullColor: "#464b5d",      // Gray
        BannerColor: "#89ddff");       // Cyan

    /// <summary>Material Palenight - Material Design palenight variant.</summary>
    public static ThemeConfig MaterialPalenight { get; } = new(
        PromptColor: "#c3e88d",        // Green
        PromptSessionColor: "#89ddff", // Cyan
        PromptContextColor: "#ffcb6b", // Yellow
        PromptCosmosAliasColor: "#f78c6c", // Orange
        PromptBlobAliasColor: "#c792ea", // Purple
        TableHeaderColor: "#82aaff",   // Blue
        TableBorderColor: "#676e95",   // Gray
        ErrorColor: "#f07178",         // Red
        WarningColor: "#ffcb6b",       // Yellow
        SuccessColor: "#c3e88d",       // Green
        DimColor: "#676e95",           // Gray
        JsonKeyColor: "#82aaff",       // Blue
        JsonStringColor: "#c3e88d",    // Green
        JsonNumberColor: "#f78c6c",    // Orange
        JsonBooleanColor: "#ffcb6b",   // Yellow
        JsonNullColor: "#676e95",      // Gray
        BannerColor: "#89ddff");       // Cyan

    // ========== Solarized ==========

    /// <summary>Solarized Dark - Ethan Schoonover's dark theme.</summary>
    public static ThemeConfig SolarizedDark { get; } = new(
        PromptColor: "#859900",        // Green
        PromptSessionColor: "#2aa198", // Cyan
        PromptContextColor: "#b58900", // Yellow
        PromptCosmosAliasColor: "#cb4b16", // Orange
        PromptBlobAliasColor: "#6c71c4", // Violet
        TableHeaderColor: "#268bd2",   // Blue
        TableBorderColor: "#586e75",   // Gray
        ErrorColor: "#dc322f",         // Red
        WarningColor: "#b58900",       // Yellow
        SuccessColor: "#859900",       // Green
        DimColor: "#586e75",           // Gray
        JsonKeyColor: "#268bd2",       // Blue
        JsonStringColor: "#2aa198",    // Cyan
        JsonNumberColor: "#d33682",    // Magenta
        JsonBooleanColor: "#b58900",   // Yellow
        JsonNullColor: "#586e75",      // Gray
        BannerColor: "#2aa198");       // Cyan

    /// <summary>Solarized Light - Ethan Schoonover's light theme.</summary>
    public static ThemeConfig SolarizedLight { get; } = new(
        PromptColor: "#859900",        // Green
        PromptSessionColor: "#2aa198", // Cyan
        PromptContextColor: "#b58900", // Yellow
        PromptCosmosAliasColor: "#cb4b16", // Orange
        PromptBlobAliasColor: "#6c71c4", // Violet
        TableHeaderColor: "#268bd2",   // Blue
        TableBorderColor: "#93a1a1",   // Gray
        ErrorColor: "#dc322f",         // Red
        WarningColor: "#b58900",       // Yellow
        SuccessColor: "#859900",       // Green
        DimColor: "#93a1a1",           // Gray
        JsonKeyColor: "#268bd2",       // Blue
        JsonStringColor: "#2aa198",    // Cyan
        JsonNumberColor: "#d33682",    // Magenta
        JsonBooleanColor: "#b58900",   // Yellow
        JsonNullColor: "#93a1a1",      // Gray
        BannerColor: "#2aa198");       // Cyan

    // ========== Preset Dictionary & Helper Methods ==========
    // NOTE: This must be declared AFTER all theme properties to ensure proper initialization order.

    /// <summary>
    /// Gets a dictionary of all available preset themes.
    /// </summary>
    public static IReadOnlyDictionary<string, ThemeConfig> All { get; } = new Dictionary<string, ThemeConfig>(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = ThemeConfig.Default,

        // Catppuccin - https://catppuccin.com
        ["catppuccin-mocha"] = CatppuccinMocha,
        ["catppuccin-macchiato"] = CatppuccinMacchiato,
        ["catppuccin-frappe"] = CatppuccinFrappe,
        ["catppuccin-latte"] = CatppuccinLatte,

        // Dracula - https://draculatheme.com
        ["dracula"] = Dracula,

        // One Dark - Atom
        ["one-dark"] = OneDark,

        // Gruvbox - https://github.com/morhetz/gruvbox
        ["gruvbox-dark"] = GruvboxDark,
        ["gruvbox-light"] = GruvboxLight,

        // Ayu - https://github.com/ayu-theme
        ["ayu-dark"] = AyuDark,
        ["ayu-mirage"] = AyuMirage,
        ["ayu-light"] = AyuLight,

        // GitHub
        ["github-dark"] = GitHubDark,
        ["github-light"] = GitHubLight,

        // Xcode
        ["xcode-dark"] = XcodeDark,
        ["xcode-light"] = XcodeLight,

        // TokyoNight - https://github.com/folke/tokyonight.nvim
        ["tokyonight-night"] = TokyoNightNight,
        ["tokyonight-storm"] = TokyoNightStorm,
        ["tokyonight-day"] = TokyoNightDay,

        // Nord - https://www.nordtheme.com
        ["nord"] = Nord,

        // VSCode
        ["vscode-dark"] = VSCodeDark,
        ["vscode-light"] = VSCodeLight,

        // Material - https://material-theme.site
        ["material-darker"] = MaterialDarker,
        ["material-ocean"] = MaterialOcean,
        ["material-palenight"] = MaterialPalenight,

        // Solarized - https://ethanschoonover.com/solarized
        ["solarized-dark"] = SolarizedDark,
        ["solarized-light"] = SolarizedLight,
    };

    /// <summary>
    /// Gets a preset theme by name, or null if not found.
    /// </summary>
    public static ThemeConfig? GetPreset(string name) =>
        All.TryGetValue(name, out var theme) ? theme : null;

    /// <summary>
    /// Gets all available preset theme names.
    /// </summary>
    public static IEnumerable<string> GetPresetNames() => All.Keys;
}
