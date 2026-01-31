using Sentinel.Application.Services;

namespace Sentinel.Application.UnitTests;

public sealed class TextCleanerTests
{
    [Fact]
    public void Clean_RemovesThreadMarkersAndQueryStrings()
    {
        var cleaner = new TextCleaner();
        var input = "Thread 1/3 Check this out https://example.com/page?utm=1  ";

        var result = cleaner.Clean(input);

        Assert.Equal("Check this out https://example.com/page", result);
    }

    [Fact]
    public void Clean_ReturnsEmpty_WhenInputIsWhitespace()
    {
        var cleaner = new TextCleaner();

        var result = cleaner.Clean("   ");

        Assert.Equal(string.Empty, result);
    }
}
