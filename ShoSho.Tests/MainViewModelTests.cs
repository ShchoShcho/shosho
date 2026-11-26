using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoSho.Core.Models;
using ShoSho.Infrastructure.Data;
using ShoSho.UI.ViewModels;
using Xunit;

namespace ShoSho.Tests;

public class MainViewModelTests
{
    private readonly DbContextOptions<ShoShoDbContext> _options;
    private readonly Mock<IDbContextFactory<ShoShoDbContext>> _mockFactory;

    public MainViewModelTests()
    {
        _options = new DbContextOptionsBuilder<ShoShoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockFactory = new Mock<IDbContextFactory<ShoShoDbContext>>();

        _mockFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ShoShoDbContext(_options));
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadItemsFromDatabase()
    {
        await using (var context = new ShoShoDbContext(_options))
        {
            context.MediaItems.Add(new MediaItem
            {
                Title = "Дюна",
                Status = MediaStatusExtensions.Planned,
                Type = "Фільм"
            });
            await context.SaveChangesAsync();
        }

        var viewModel = new MainViewModel(_mockFactory.Object);

        await viewModel.InitializeAsync();

        Assert.Single(viewModel.Items);
        Assert.Equal("Дюна", viewModel.Items.First().Title);
        Assert.False(viewModel.IsLoading); 
    }

    [Fact]
    public async Task FilterItems_ByStatus_ShouldUpdateVisibleCount()
    {
        await using (var context = new ShoShoDbContext(_options))
        {
            context.MediaItems.Add(new MediaItem { Title = "Дюна", Status = MediaStatusExtensions.Planned, Type = "Фільм" });
            context.MediaItems.Add(new MediaItem { Title = "Футурама", Status = MediaStatusExtensions.Completed, Type = "Серіал" });
            await context.SaveChangesAsync();
        }

        var viewModel = new MainViewModel(_mockFactory.Object);
        await viewModel.InitializeAsync();

        Assert.StartsWith("Завантажено", viewModel.StatusMessage);

        viewModel.FilterItems(MediaStatusExtensions.Planned);

        Assert.Equal(MediaStatusExtensions.Planned, viewModel.SelectedStatusFilter);
        Assert.Equal(1, viewModel.VisibleCount); 
    }

    [Fact]
    public async Task UpdateItemStatusAsync_ShouldChangeStatusSuccessfully()
    {
        await using (var context = new ShoShoDbContext(_options))
        {
            context.MediaItems.Add(new MediaItem { Title = "Матриця", Status = MediaStatusExtensions.Planned, Type = "Фільм" });
            await context.SaveChangesAsync();
        }

        var viewModel = new MainViewModel(_mockFactory.Object);
        await viewModel.InitializeAsync();
        var itemToUpdate = viewModel.Items.First();

        itemToUpdate.Status = MediaStatusExtensions.Completed;
        await viewModel.UpdateItemStatusAsync(itemToUpdate);

        Assert.Equal(MediaStatusExtensions.Completed, viewModel.Items.First().Status);
        Assert.Contains("успішно змінено", viewModel.StatusMessage);
    }

    [Fact]
    public async Task FilterItems_BySearchText_ShouldUpdateVisibleCount()
    {
        await using (var context = new ShoShoDbContext(_options))
        {
            context.MediaItems.Add(new MediaItem { Title = "Інтерстеллар", Status = MediaStatusExtensions.Completed, Type = "Фільм" });
            context.MediaItems.Add(new MediaItem { Title = "Початок", Status = MediaStatusExtensions.Completed, Type = "Фільм" });
            await context.SaveChangesAsync();
        }

        var viewModel = new MainViewModel(_mockFactory.Object);
        await viewModel.InitializeAsync();

        viewModel.SearchText = "Інтер";

        Assert.Equal(1, viewModel.VisibleCount);
    }

    [Fact]
    public async Task SaveReviewAsync_WithInvalidRating_ShouldShowErrorMessage()
    {
        await using (var context = new ShoShoDbContext(_options))
        {
            var item = new MediaItem { Title = "Дюна", Status = MediaStatusExtensions.Completed, Type = "Фільм" };
            item.Review = new Review { MediaItemId = item.Id, Rating = 5, Text = "Супер" };
            context.MediaItems.Add(item);
            await context.SaveChangesAsync();
        }

        var viewModel = new MainViewModel(_mockFactory.Object);
        await viewModel.InitializeAsync();
        var itemToReview = viewModel.Items.First();

        itemToReview.Review!.Rating = 10;
        await viewModel.SaveReviewAsync(itemToReview);

        Assert.Contains("Оцінка має бути від 1 до 5", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ApplySorting_ByHighestRating_ShouldSortItemsCorrectly()
    {
        await using (var context = new ShoShoDbContext(_options))
        {
            var item1 = new MediaItem { Title = "Непопулярний", Status = MediaStatusExtensions.Completed, Type = "Фільм" }; item1.Review = new Review { MediaItemId = item1.Id, Rating = 2, Text = "Норм" };
            var item2 = new MediaItem { Title = "Шедевр", Status = MediaStatusExtensions.Completed, Type = "Фільм" };
            item2.Review = new Review { MediaItemId = item2.Id, Rating = 5, Text = "Вау" };

            context.MediaItems.AddRange(item1, item2);
            await context.SaveChangesAsync();
        }

        var viewModel = new MainViewModel(_mockFactory.Object);
        await viewModel.InitializeAsync();

        viewModel.SelectedRatingSort = "Від найвищого";

        Assert.Equal("Шедевр", viewModel.Items.First().Title);
    }

    [Fact]
    public void SelectCategory_ShouldUpdateSelectedCategory()
    {
        var viewModel = new MainViewModel(_mockFactory.Object);
        var targetCategory = viewModel.Categories.Skip(1).First(); 

        viewModel.SelectCategoryCommand.Execute(targetCategory);

        Assert.Equal(targetCategory, viewModel.SelectedCategory);
        Assert.True(targetCategory.IsSelected);
    }
}