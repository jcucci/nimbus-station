using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class ThemeCommandTests
{
    private readonly StubSessionStateManager _sessionStateManager;
    private readonly StubConfigurationService _configurationService;
    private readonly ThemeCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public ThemeCommandTests()
    {
        _sessionStateManager = new StubSessionStateManager();
        _configurationService = new StubConfigurationService();
        _command = new ThemeCommand(_configurationService);
        _outputWriter = new CaptureOutputWriter();
    }

    [Fact]
    public void Name_ReturnsTheme()
    {
        Assert.Equal("theme", _command.Name);
    }

    [Fact]
    public void Subcommands_ContainsExpectedValues()
    {
        Assert.Contains("list", _command.Subcommands);
        Assert.Contains("ls", _command.Subcommands);
        Assert.Contains("preview", _command.Subcommands);
        Assert.Contains("current", _command.Subcommands);
        Assert.Contains("presets", _command.Subcommands);
    }

    [Fact]
    public void Usage_ReturnsExpectedFormat()
    {
        Assert.Contains("theme", _command.Usage);
        Assert.Contains("list", _command.Usage);
        Assert.Contains("preview", _command.Usage);
        Assert.Contains("current", _command.Usage);
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_ShowsCurrent()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
        Assert.IsType<ThemeConfig>(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_Current_ReturnsSuccess()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["current"], context);

        Assert.True(result.Success);
        Assert.IsType<ThemeConfig>(result.Data);
        Assert.Contains("Current Theme Configuration", _outputWriter.GetOutput());
    }

    [Fact]
    public async Task ExecuteAsync_List_ReturnsAllPresets()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["list"], context);

        Assert.True(result.Success);
        var presets = Assert.IsType<List<string>>(result.Data);
        Assert.True(presets.Count >= 24); // We have 24 presets
        Assert.Contains("default", presets);
        Assert.Contains("catppuccin-mocha", presets);
        Assert.Contains("dracula", presets);
    }

    [Fact]
    public async Task ExecuteAsync_Ls_ReturnsAllPresets()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["ls"], context);

        Assert.True(result.Success);
        Assert.IsType<List<string>>(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_Presets_ReturnsAllPresets()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["presets"], context);

        Assert.True(result.Success);
        Assert.IsType<List<string>>(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_List_OutputContainsThemes()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["list"], context);

        Assert.Contains("theme(s) available", _outputWriter.GetOutput());
    }

    [Fact]
    public async Task ExecuteAsync_Preview_ValidTheme_ReturnsSuccess()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["preview", "dracula"], context);

        Assert.True(result.Success);
        Assert.IsType<ThemeConfig>(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_Preview_ValidTheme_ShowsPreview()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["preview", "catppuccin-mocha"], context);

        Assert.Contains("Preview: catppuccin-mocha", _outputWriter.GetOutput());
        Assert.Contains("Sample prompt:", _outputWriter.GetOutput());
        Assert.Contains("Sample messages:", _outputWriter.GetOutput());
        Assert.Contains("Sample JSON:", _outputWriter.GetOutput());
    }

    [Fact]
    public async Task ExecuteAsync_Preview_InvalidTheme_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["preview", "nonexistent-theme"], context);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Preview_NoThemeName_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["preview"], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownSubcommand_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["unknown"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown subcommand", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Preview_CaseInsensitive_ReturnsSuccess()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["preview", "DRACULA"], context);

        Assert.True(result.Success);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("catppuccin-mocha")]
    [InlineData("catppuccin-macchiato")]
    [InlineData("catppuccin-frappe")]
    [InlineData("catppuccin-latte")]
    [InlineData("dracula")]
    [InlineData("one-dark")]
    [InlineData("gruvbox-dark")]
    [InlineData("gruvbox-light")]
    [InlineData("nord")]
    [InlineData("solarized-dark")]
    [InlineData("solarized-light")]
    public async Task ExecuteAsync_Preview_AllMajorThemes_ReturnsSuccess(string themeName)
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["preview", themeName], context);

        Assert.True(result.Success);
        Assert.IsType<ThemeConfig>(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_Current_ShowsConfigHint()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["current"], context);

        // The config hint is written via WriteLine(), not part of the table
        Assert.Contains("config.toml", _outputWriter.GetOutput());
    }

    [Fact]
    public async Task ExecuteAsync_Preview_ShowsConfigHint()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["preview", "nord"], context);

        // The config hint is written via WriteLine()
        Assert.Contains("config.toml", _outputWriter.GetOutput());
        Assert.Contains("theme", _outputWriter.GetOutput());
        Assert.Contains("preset", _outputWriter.GetOutput());
    }

    [Fact]
    public void HelpMetadata_IsNotNull() => Assert.NotNull(_command.HelpMetadata);
}
