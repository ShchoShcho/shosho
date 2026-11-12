using ShoSho.Core.Models;
using Xunit;

namespace ShoSho.Tests;

public class MediaStatusExtensionsTests
{
    [Fact]
    public void ToDisplayName_WhenStatusIsPlanned_ReturnsHochu()
    {
        var status = MediaStatus.Planned;

        var result = status.ToDisplayName();

        Assert.Equal("Хочу", result);
    }

    [Fact]
    public void ToDisplayName_WhenStatusIsCompleted_ReturnsVzheVso()
    {
        var status = MediaStatus.Completed;

        var result = status.ToDisplayName();

        Assert.Equal("Вже всьо", result);
    }
}