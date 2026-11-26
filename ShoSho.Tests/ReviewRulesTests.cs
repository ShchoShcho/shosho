using System;
using Xunit;
using ShoSho.Core.Services;
using ShoSho.Core.Models;

namespace ShoSho.Tests;

public class ReviewRulesTests
{
    [Fact]
    public void ValidateChange_WithCorrectRating_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
            ReviewRules.ValidateChange(MediaStatusExtensions.Completed, 4, "Круто", 4, "Круто")
        );

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateChange_WithInvalidRating_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ReviewRules.ValidateChange(MediaStatusExtensions.Completed, 6, "Тест", 4, "Круто")
        );
    }
}