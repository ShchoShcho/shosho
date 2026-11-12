using Xunit;
using ShoSho.Core.Models;

namespace ShoSho.Tests;

public class CoreCoverageTests
{
    [Fact]
    public void Tag_Model_ShouldStoreAndRetrieveProperties()
    {
        var tag = new Tag
        {
            Id = System.Guid.NewGuid(),
            Name = "драма"
        };

        Assert.Equal("драма", tag.Name);
        Assert.NotEqual(System.Guid.Empty, tag.Id);
    }

    [Fact]
    public void MediaStatusExtensions_AllConstants_ShouldBeValid()
    {
        var statuses = new[]
        {
            MediaStatusExtensions.Planned,
            MediaStatusExtensions.InProgress,
            MediaStatusExtensions.Completed,
            MediaStatusExtensions.Postponed
        };

        foreach (var status in statuses)
        {
            Assert.False(string.IsNullOrWhiteSpace(status));
        }
    }
}