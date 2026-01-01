using NimbusStation.Core.Errors;
using NimbusStation.Providers.Azure.Errors;

namespace NimbusStation.Tests.Providers.Azure.Errors;

public class AzureErrorMapperTests
{
    // Note: CosmosException tests are difficult to write because CosmosException
    // doesn't have a public constructor. The mapping logic is tested indirectly
    // through integration tests. These unit tests focus on the CLI error mapping
    // and InvalidOperationException mapping which are easier to test.

    [Fact]
    public void FromInvalidOperation_AliasNotFound_ReturnsConfigurationError()
    {
        var ex = new InvalidOperationException("Cosmos alias 'unknown' not found");

        var error = AzureErrorMapper.FromInvalidOperation(ex, "unknown");

        Assert.Equal(ErrorCategory.Configuration, error.Category);
        Assert.Contains("unknown", error.Message);
    }

    [Fact]
    public void FromInvalidOperation_EnvironmentVariable_ReturnsConfigurationError()
    {
        var ex = new InvalidOperationException("Environment variable 'COSMOS_KEY' is not set");

        var error = AzureErrorMapper.FromInvalidOperation(ex);

        Assert.Equal(ErrorCategory.Configuration, error.Category);
        Assert.NotNull(error.Suggestions);
    }

    [Fact]
    public void FromInvalidOperation_GenericMessage_ReturnsConfigurationError()
    {
        var ex = new InvalidOperationException("Some other error");

        var error = AzureErrorMapper.FromInvalidOperation(ex);

        Assert.Equal(ErrorCategory.Configuration, error.Category);
        Assert.Equal("Some other error", error.Message);
    }

    [Theory]
    [InlineData("Please run 'az login'")]
    [InlineData("not logged in")]
    [InlineData("AADSTS700016: Application error")]
    public void FromCliError_AuthenticationPatterns_ReturnsAuthError(string errorMessage)
    {
        var error = AzureErrorMapper.FromCliError(errorMessage);

        Assert.Equal(ErrorCategory.Authentication, error.Category);
    }

    [Theory]
    [InlineData("ResourceNotFound: The container was not found")]
    [InlineData("Container not found in storage account")]
    [InlineData("The specified blob does not exist")]
    public void FromCliError_NotFoundPatterns_ReturnsNotFoundError(string errorMessage)
    {
        var error = AzureErrorMapper.FromCliError(errorMessage);

        Assert.Equal(ErrorCategory.NotFound, error.Category);
    }

    [Theory]
    [InlineData("AuthorizationFailed: Access denied")]
    [InlineData("User does not have authorization to perform this action")]
    public void FromCliError_AuthorizationPatterns_ReturnsAuthError(string errorMessage)
    {
        var error = AzureErrorMapper.FromCliError(errorMessage);

        Assert.Equal(ErrorCategory.Authentication, error.Category);
    }

    [Theory]
    [InlineData("network unreachable")]
    [InlineData("connection refused")]
    [InlineData("request timeout")]
    public void FromCliError_NetworkPatterns_ReturnsNetworkError(string errorMessage)
    {
        var error = AzureErrorMapper.FromCliError(errorMessage);

        Assert.Equal(ErrorCategory.Network, error.Category);
    }

    [Fact]
    public void FromCliError_NullOrEmpty_ReturnsGeneralError()
    {
        var error1 = AzureErrorMapper.FromCliError(null);
        var error2 = AzureErrorMapper.FromCliError("");
        var error3 = AzureErrorMapper.FromCliError("   ");

        Assert.Equal(ErrorCategory.General, error1.Category);
        Assert.Equal(ErrorCategory.General, error2.Category);
        Assert.Equal(ErrorCategory.General, error3.Category);
    }

    [Fact]
    public void FromCliError_GenericError_ReturnsGeneralError()
    {
        var error = AzureErrorMapper.FromCliError("Something went wrong");

        Assert.Equal(ErrorCategory.General, error.Category);
        Assert.Equal("Something went wrong", error.Message);
    }

    [Fact]
    public void FromCliError_WithResourceName_IncludesInDetails()
    {
        var error = AzureErrorMapper.FromCliError(
            "ResourceNotFound: Container not found",
            resourceName: "my-container");

        Assert.NotNull(error.Details);
        Assert.Contains("my-container", error.Details);
    }

    [Fact]
    public void FromException_InvalidOperationException_DelegatesToFromInvalidOperation()
    {
        var ex = new InvalidOperationException("Alias not found");

        var error = AzureErrorMapper.FromException(ex);

        Assert.Equal(ErrorCategory.Configuration, error.Category);
    }

    [Fact]
    public void FromException_OperationCanceledException_ReturnsCancelledError()
    {
        var ex = new OperationCanceledException();

        var error = AzureErrorMapper.FromException(ex);

        Assert.Equal(ErrorCategory.Cancelled, error.Category);
    }

    [Fact]
    public void FromException_GenericException_ReturnsGeneralError()
    {
        var ex = new Exception("Generic error");

        var error = AzureErrorMapper.FromException(ex);

        Assert.Equal(ErrorCategory.General, error.Category);
        Assert.Equal("Generic error", error.Message);
    }

    [Fact]
    public void FromException_NetworkRelatedMessage_ReturnsNetworkError()
    {
        var ex = new Exception("Unable to connect to remote server");

        var error = AzureErrorMapper.FromException(ex);

        Assert.Equal(ErrorCategory.Network, error.Category);
    }
}
