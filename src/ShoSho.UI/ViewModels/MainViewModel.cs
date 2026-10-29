using System.Collections.ObjectModel;
using System.Windows;
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
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<MediaItem> Items { get; } = new();

    public MainViewModel(IDbContextFactory<ShoShoDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    // Конструктор за замовчуванням для дизайнера XAML (Design-time)
    public MainViewModel()
        : this(new DesignTimeDbContextFactory())
    {
        // Тестовий запис для перевірки плейсхолдера та сітки без запуску програми
        Items.Add(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Тестовий фільм",
            PosterUrl = null,
            Status = MediaStatusExtensions.Planned,
            CreatedAt = DateTime.UtcNow
        });
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Підключення до бази даних...";

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            
            // Автоматичне застосування міграцій при старті
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

    // Команда відкриття модального вікна та збереження нового запису
    [RelayCommand]
    public async Task AddItemAsync()
    {
        var addWindow = new AddItemWindow();
        
        if (Application.Current.MainWindow != null && Application.Current.MainWindow != addWindow)
        {
            addWindow.Owner = Application.Current.MainWindow;
        }

        // Якщо користувач натиснув «Зберегти» у модальному вікні
        if (addWindow.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Збереження в базу даних...";

                var newItem = new MediaItem
                {
                    Id = Guid.NewGuid(),
                    Title = addWindow.ItemTitle,
                    Type = addWindow.MediaType,
                    Status = addWindow.Status,
                    PosterUrl = addWindow.PosterUrl,
                    Description = addWindow.Note,
                    Tags = addWindow.Tags,
                    Rating = addWindow.Status == "Завершено" ? addWindow.Rating : (int?)null,
                    Review = addWindow.Status == "Завершено" ? addWindow.Review : string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                await using var db = await _dbContextFactory.CreateDbContextAsync();
                db.MediaItems.Add(newItem);
                await db.SaveChangesAsync();

                // Додаємо картку на початок списку в UI
                Items.Insert(0, newItem);
                StatusMessage = $"Картку «{newItem.Title}» успішно збережено!";
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
    }

    private sealed class DesignTimeDbContextFactory : IDbContextFactory<ShoShoDbContext>
    {
        public ShoShoDbContext CreateDbContext()
        {
            return new ShoShoDbContext();
        }
    }
}