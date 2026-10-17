using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using ShoSho.Core.Models;
using ShoSho.Infrastructure.Data;

namespace ShoSho.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDbContextFactory<ShoShoDbContext> _dbContextFactory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
    private string _newItemTitle = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<MediaItem> Items { get; } = new();

    public MainViewModel(IDbContextFactory<ShoShoDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    // Default parameterless constructor for XAML design-time / fallback
    public MainViewModel()
        : this(new DesignTimeDbContextFactory())
    {
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Підключення до бази даних...";

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            
            // Застосовуємо міграції автоматично при старті, щоб БД та таблиці були гарантовано створені
            await db.Database.MigrateAsync();

            var items = await db.MediaItems
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            StatusMessage = $"Завантажено карток: {Items.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Помилка підключення до БД: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanAddItem => !string.IsNullOrWhiteSpace(NewItemTitle) && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    public async Task AddItemAsync()
    {
        var title = NewItemTitle.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Збереження в базу даних...";

            var newItem = new MediaItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = string.Empty,
                Status = MediaStatusExtensions.Planned,
                CreatedAt = DateTime.UtcNow
            };

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            db.MediaItems.Add(newItem);
            await db.SaveChangesAsync();

            // Додаємо новий елемент на початок списку
            Items.Insert(0, newItem);
            NewItemTitle = string.Empty;
            StatusMessage = $"Картку «{title}» успішно збережено!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не вдалося зберегти картку: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private sealed class DesignTimeDbContextFactory : IDbContextFactory<ShoShoDbContext>
    {
        public ShoShoDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ShoShoDbContext>()
                .UseNpgsql(ShoShoDbContext.DefaultConnectionString)
                .Options;
            return new ShoShoDbContext(options);
        }
    }
}

