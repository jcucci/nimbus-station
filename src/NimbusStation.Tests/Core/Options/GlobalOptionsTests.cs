using NimbusStation.Core.Options;

namespace NimbusStation.Tests.Core.Options;

public class GlobalOptionsTests
{
    [Fact]
    public void Parse_NoFlags_ReturnsDefaults()
    {
        var args = new[] { "session", "start", "TICKET-123" };
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.False(options.Verbose);
        Assert.False(options.Quiet);
        Assert.False(options.NoColor);
        Assert.False(options.YesToAll);
        Assert.Equal(args, remaining);
    }

    [Fact]
    public void Parse_VerboseFlag_SetsVerbose()
    {
        var args = new[] { "--verbose", "cosmos", "query" };
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.True(options.Verbose);
        Assert.Equal(new[] { "cosmos", "query" }, remaining);
    }

    [Fact]
    public void Parse_QuietFlag_SetsQuiet()
    {
        var args = new[] { "blob", "list", "--quiet" };
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.True(options.Quiet);
        Assert.Equal(new[] { "blob", "list" }, remaining);
    }

    [Fact]
    public void Parse_NoColorFlag_SetsNoColor()
    {
        var args = new[] { "--no-color", "session", "list" };
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.True(options.NoColor);
        Assert.Equal(new[] { "session", "list" }, remaining);
    }

    [Fact]
    public void Parse_YesFlag_SetsYesToAll()
    {
        var args = new[] { "session", "delete", "TICKET-123", "--yes" };
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.True(options.YesToAll);
        Assert.Equal(new[] { "session", "delete", "TICKET-123" }, remaining);
    }

    [Fact]
    public void Parse_MultipleFlags_SetsAll()
    {
        var args = new[] { "--verbose", "--no-color", "--yes", "cosmos", "query" };
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.True(options.Verbose);
        Assert.False(options.Quiet);
        Assert.True(options.NoColor);
        Assert.True(options.YesToAll);
        Assert.Equal(new[] { "cosmos", "query" }, remaining);
    }

    [Fact]
    public void Parse_FlagsAnyPosition_Recognized()
    {
        var args = new[] { "cosmos", "--verbose", "query", "--quiet", "SELECT *" };
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.True(options.Verbose);
        Assert.True(options.Quiet);
        Assert.Equal(new[] { "cosmos", "query", "SELECT *" }, remaining);
    }

    [Theory]
    [InlineData("--VERBOSE")]
    [InlineData("--Verbose")]
    [InlineData("--verbose")]
    public void Parse_CaseInsensitive_Recognized(string flag)
    {
        var args = new[] { flag, "help" };
        
        var (options, _) = GlobalOptions.Parse(args);
        
        Assert.True(options.Verbose);
    }

    [Fact]
    public void Parse_EmptyArgs_ReturnsDefaults()
    {
        var args = Array.Empty<string>();
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.False(options.Verbose);
        Assert.False(options.Quiet);
        Assert.False(options.NoColor);
        Assert.False(options.YesToAll);
        Assert.Empty(remaining);
    }

    [Fact]
    public void Parse_UnknownFlags_PreservedInRemaining()
    {
        var args = new[] { "--unknown", "--verbose", "--other-flag" };
        
        var (options, remaining) = GlobalOptions.Parse(args);
        
        Assert.True(options.Verbose);
        Assert.Equal(new[] { "--unknown", "--other-flag" }, remaining);
    }

    [Fact]
    public void Default_ReturnsAllFalse()
    {
        var options = GlobalOptions.Default;
        
        Assert.False(options.Verbose);
        Assert.False(options.Quiet);
        Assert.False(options.NoColor);
        Assert.False(options.YesToAll);
    }
}
