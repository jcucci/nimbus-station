using NimbusStation.Cli.Output;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Infrastructure.Output;

namespace NimbusStation.Tests.Cli.Output;

public sealed class CommandHelpRendererTests
{
    private static readonly ThemeConfig DefaultTheme = ThemeConfig.Default;

    [Fact]
    public void Render_WithMetadata_IncludesCommandNameAndDescription()
    {
        var command = new FakeCommand(new CommandHelpMetadata());
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        var text = output.GetOutput();
        Assert.Contains("fake", text);
        Assert.Contains("A fake command", text);
    }

    [Fact]
    public void Render_WithMetadata_IncludesUsage()
    {
        var command = new FakeCommand(new CommandHelpMetadata());
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        Assert.Contains("fake [args]", output.GetOutput());
    }

    [Fact]
    public void Render_WithSubcommands_RendersSubcommandSection()
    {
        var metadata = new CommandHelpMetadata
        {
            Subcommands =
            [
                new("start <name>", "Start something"),
                new("stop", "Stop something")
            ]
        };
        var command = new FakeCommand(metadata);
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        var text = output.GetOutput();
        Assert.Contains("Subcommands", text);
        Assert.Contains("start <name>", text);
        Assert.Contains("Start something", text);
        Assert.Contains("stop", text);
        Assert.Contains("Stop something", text);
    }

    [Fact]
    public void Render_WithFlags_RendersFlagSection()
    {
        var metadata = new CommandHelpMetadata
        {
            Flags =
            [
                new("--max-items N", "Limit results")
            ]
        };
        var command = new FakeCommand(metadata);
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        var text = output.GetOutput();
        Assert.Contains("Flags", text);
        Assert.Contains("--max-items N", text);
        Assert.Contains("Limit results", text);
    }

    [Fact]
    public void Render_WithExamples_RendersExampleSection()
    {
        var metadata = new CommandHelpMetadata
        {
            Examples =
            [
                new("fake start foo", "Start foo"),
                new("fake stop", "Stop everything")
            ]
        };
        var command = new FakeCommand(metadata);
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        var text = output.GetOutput();
        Assert.Contains("Examples", text);
        Assert.Contains("fake start foo", text);
        Assert.Contains("Start foo", text);
    }

    [Fact]
    public void Render_WithNotes_RendersNotes()
    {
        var metadata = new CommandHelpMetadata
        {
            Notes = "Some important note."
        };
        var command = new FakeCommand(metadata);
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        Assert.Contains("Some important note.", output.GetOutput());
    }

    [Fact]
    public void Render_NullMetadata_RendersBasicHelpOnly()
    {
        var command = new FakeCommand(helpMetadata: null);
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        var text = output.GetOutput();
        Assert.Contains("fake", text);
        Assert.Contains("A fake command", text);
        Assert.Contains("fake [args]", text);
        Assert.DoesNotContain("Subcommands", text);
        Assert.DoesNotContain("Flags", text);
        Assert.DoesNotContain("Examples", text);
    }

    [Fact]
    public void Render_EmptyMetadata_RendersBasicHelpOnly()
    {
        var command = new FakeCommand(new CommandHelpMetadata());
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        var text = output.GetOutput();
        Assert.Contains("fake", text);
        Assert.DoesNotContain("Subcommands", text);
        Assert.DoesNotContain("Flags", text);
        Assert.DoesNotContain("Examples", text);
    }

    [Fact]
    public void Render_PartialMetadata_RendersOnlyPopulatedSections()
    {
        var metadata = new CommandHelpMetadata
        {
            Examples =
            [
                new("fake run", "Run it")
            ]
        };
        var command = new FakeCommand(metadata);
        var output = new CaptureOutputWriter();

        CommandHelpRenderer.Render(command, output, DefaultTheme);

        var text = output.GetOutput();
        Assert.DoesNotContain("Subcommands", text);
        Assert.DoesNotContain("Flags", text);
        Assert.Contains("Examples", text);
        Assert.Contains("fake run", text);
    }

    private sealed class FakeCommand : ICommand
    {
        public string Name => "fake";
        public string Description => "A fake command";
        public string Usage => "fake [args]";
        public CommandHelpMetadata? HelpMetadata { get; }

        public FakeCommand(CommandHelpMetadata? helpMetadata) => HelpMetadata = helpMetadata;

        public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult.Ok());
    }
}
