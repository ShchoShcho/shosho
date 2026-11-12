using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShoSho.Core.Models;
using ShoSho.Infrastructure.Data;
using Xunit;

namespace ShoSho.Tests;

public class DatabaseTests
{
    [Fact]
    public void AddMediaItem_ShouldSaveToDatabaseSuccessfully()
    {
        var options = new DbContextOptionsBuilder<ShoShoDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_ShoSho_Db_Add")
            .Options;

        using var context = new ShoShoDbContext(options);

        var newItem = new MediaItem
        {
            Title = "Інтерстеллар",
            Status = MediaStatus.Planned.ToString()
        };

        context.MediaItems.Add(newItem);
        context.SaveChanges();

        Assert.Equal(1, context.MediaItems.Count());
        Assert.Equal("Інтерстеллар", context.MediaItems.First().Title);
    }

    [Fact]
    public void RemoveMediaItem_ShouldDeleteFromDatabaseSuccessfully()
    {
        var options = new DbContextOptionsBuilder<ShoShoDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_ShoSho_Db_Remove") 
            .Options;

        using var context = new ShoShoDbContext(options);
        var itemToDelete = new MediaItem
        {
            Title = "Матриця",
            Status = MediaStatus.Planned.ToString()
        };
        context.MediaItems.Add(itemToDelete);
        context.SaveChanges();

        context.MediaItems.Remove(itemToDelete);
        context.SaveChanges();

        Assert.Equal(0, context.MediaItems.Count());
    }

    [Fact]
    public void UpdateMediaItem_ShouldModifyDatabaseSuccessfully()
    {
        var options = new DbContextOptionsBuilder<ShoShoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ShoShoDbContext(options);
        var item = new MediaItem { Title = "Стара назва", Status = MediaStatusExtensions.Planned };
        context.MediaItems.Add(item);
        context.SaveChanges();

        item.Title = "Нова назва";
        context.SaveChanges();

        var updatedItem = context.MediaItems.First();
        Assert.Equal("Нова назва", updatedItem.Title);
    }
}