using NimbusStation.Core.Formatting;

namespace NimbusStation.Tests.Core.Formatting;

public sealed class SizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    public void Format_Bytes_ReturnsWholeNumber(long bytes, string expected)
    {
        var result = SizeFormatter.Format(bytes);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(2048, "2.0 KB")]
    [InlineData(1048575, "1024.0 KB")]
    public void Format_Kilobytes_ReturnsOneDecimal(long bytes, string expected)
    {
        var result = SizeFormatter.Format(bytes);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1572864, "1.5 MB")]
    [InlineData(10485760, "10.0 MB")]
    [InlineData(1073741823, "1024.0 MB")]
    public void Format_Megabytes_ReturnsOneDecimal(long bytes, string expected)
    {
        var result = SizeFormatter.Format(bytes);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1073741824, "1.0 GB")]
    [InlineData(1610612736, "1.5 GB")]
    [InlineData(10737418240, "10.0 GB")]
    public void Format_Gigabytes_ReturnsOneDecimal(long bytes, string expected)
    {
        var result = SizeFormatter.Format(bytes);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1099511627776, "1.0 TB")]
    [InlineData(1649267441664, "1.5 TB")]
    [InlineData(10995116277760, "10.0 TB")]
    public void Format_Terabytes_ReturnsOneDecimal(long bytes, string expected)
    {
        var result = SizeFormatter.Format(bytes);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_VeryLargeValue_StaysInTerabytes()
    {
        // 1000 TB - should stay in TB, not overflow
        var result = SizeFormatter.Format(1099511627776000);
        Assert.Equal("1000.0 TB", result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(long.MinValue)]
    public void Format_NegativeValue_ReturnsZeroBytes(long bytes)
    {
        var result = SizeFormatter.Format(bytes);
        Assert.Equal("0 B", result);
    }

    [Fact]
    public void Format_ExactBoundary_1024Bytes_ReturnsKilobytes()
    {
        var result = SizeFormatter.Format(1024);
        Assert.Equal("1.0 KB", result);
    }

    [Fact]
    public void Format_JustUnderBoundary_1023Bytes_ReturnsBytes()
    {
        var result = SizeFormatter.Format(1023);
        Assert.Equal("1023 B", result);
    }
}
