using NimbusStation.Core.Commands;

namespace NimbusStation.Tests.Core.Commands;

public sealed class CommandResultTests
{
    [Fact]
    public void Ok_ReturnsSuccessTrue()
    {
        var result = CommandResult.Ok();

        Assert.True(result.Success);
        Assert.False(result.IsExitSignal);
    }

    [Fact]
    public void Ok_WithMessage_ReturnsMessage()
    {
        var result = CommandResult.Ok("Done");

        Assert.True(result.Success);
        Assert.Equal("Done", result.Message);
    }

    [Fact]
    public void Ok_WithData_ReturnsData()
    {
        var data = new { Id = 1 };
        var result = CommandResult.Ok(data, "Done");

        Assert.True(result.Success);
        Assert.Same(data, result.Data);
    }

    [Fact]
    public void Error_ReturnsSuccessFalse()
    {
        var result = CommandResult.Error("Failed");

        Assert.False(result.Success);
        Assert.Equal("Failed", result.Message);
        Assert.False(result.IsExitSignal);
    }

    [Fact]
    public void Exit_ReturnsIsExitSignalTrue()
    {
        var result = CommandResult.Exit();

        Assert.True(result.IsExitSignal);
        Assert.True(result.Success);
    }

    [Fact]
    public void Exit_ReturnsDefaultMessage()
    {
        var result = CommandResult.Exit();

        Assert.Equal("Goodbye!", result.Message);
    }

    [Fact]
    public void Exit_WithCustomMessage_ReturnsMessage()
    {
        var result = CommandResult.Exit("See you later!");

        Assert.Equal("See you later!", result.Message);
        Assert.True(result.IsExitSignal);
    }

    [Fact]
    public void Exit_WithExitCode_StoresInData()
    {
        var result = CommandResult.Exit(exitCode: 42, message: "Exiting with code");

        Assert.Equal(42, result.Data);
        Assert.True(result.IsExitSignal);
        Assert.Equal("Exiting with code", result.Message);
    }

    [Fact]
    public void Exit_WithExitCode_DefaultMessage()
    {
        var result = CommandResult.Exit(exitCode: 1);

        Assert.Equal(1, result.Data);
        Assert.Equal("Goodbye!", result.Message);
    }

    [Fact]
    public void Ok_IsExitSignalIsFalse()
    {
        var result = CommandResult.Ok("Success");

        Assert.False(result.IsExitSignal);
    }

    [Fact]
    public void Error_IsExitSignalIsFalse()
    {
        var result = CommandResult.Error("Error");

        Assert.False(result.IsExitSignal);
    }
}
