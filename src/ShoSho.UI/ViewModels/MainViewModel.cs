using ShoSho.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using ShoSho.Core.Models;
using ShoSho.Infrastructure.Data;
using System.ComponentModel;
using System.Windows.Data;

namespace ShoSho.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDbContextFactory<ShoShoDbContext> _dbContextFactory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
    private string _newItemTitle = string.Empty;

    [ObservableProperty]
    private string _newItemTags = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchTagFilter = string.Empty;

    [ObservableProperty]
    private string _selectedStatusFilter = string.Empty;

    public ObservableCollection<MediaItem> Items { get; } = new();

    public List<string> AvailableStatuses { get; } = new()
    {
        MediaStatusExtensions.Planned,
        MediaStatusExtensions.InProgress,
        MediaStatusExtensions.Completed,
        MediaStatusExtensions.Postponed
    };

    public MainViewModel(IDbContextFactory<ShoShoDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    // Конструктор за замовчуванням для дизайнера XAML (Design-time)
    public MainViewModel()
        : this(new DesignTimeDbContextFactory())
    {
        Items.Add(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Тестовий фільм",
            PosterUrl = null,
            Status = MediaStatusExtensions.Planned,
            CreatedAt = DateTime.UtcNow
        });
    }

    partial void OnSearchTagFilterChanged(string value)
    {
        var view = CollectionViewSource.GetDefaultView(Items);

        if (string.IsNullOrWhiteSpace(value))
        {
            view.Filter = null;
        }
        else
        {
            var searchTarget = value.Trim().ToLower();
            view.Filter = obj =>
            {
                if (obj is MediaItem item)
                {
                    return item.Tags.Any(t => t.Name.ToLower().Contains(searchTarget));
                }
                return false;
            };
        }
    }

    partial void OnSearchTagFilterChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedStatusFilterChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var view = CollectionViewSource.GetDefaultView(Items);

        if (string.IsNullOrWhiteSpace(SearchTagFilter) &&
            (string.IsNullOrEmpty(SelectedStatusFilter) || SelectedStatusFilter == "Усі" || SelectedStatusFilter == "Усі статуси"))
        {
            view.Filter = null;
            return;
        }

        view.Filter = obj =>
        {
            if (obj is not MediaItem item) return false;

            bool matchesStatus = string.IsNullOrEmpty(SelectedStatusFilter) ||
                                 SelectedStatusFilter == "Усі" ||
                                 SelectedStatusFilter == "Усі статуси" ||
                                 item.Status == SelectedStatusFilter;

            bool matchesTag = true;
            if (!string.IsNullOrWhiteSpace(SearchTagFilter))
            {
                var searchTarget = SearchTagFilter.Trim().ToLower();
                matchesTag = item.Tags.Any(t => t.Name.ToLower().Contains(searchTarget));
            }

            return matchesStatus && matchesTag;
        };
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Підключення до бази даних...";

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await db.Database.MigrateAsync();

            var items = await db.MediaItems
                .Include(m => m.Tags)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            Items.Clear();
            foreach (var item in items)
            {
                // Кешуємо зображення: якщо це веб-посилання, завантажуємо локально і підміняємо шлях для UI
                if (!string.IsNullOrWhiteSpace(item.PosterUrl) && item.PosterUrl.StartsWith("http"))
                {
                    var localPath = await ImageCacheService.GetLocalPosterAsync(item.PosterUrl);
                    if (localPath != null)
                    {
                        item.PosterUrl = localPath;
                    }
                }
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
            IsLoading = true;
            StatusMessage = "Збереження в базу даних...";

            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var tagsToAssign = new List<Tag>();
            if (!string.IsNullOrWhiteSpace(NewItemTags))
            {
                var tagNames = NewItemTags.Split(new[] { ',', '#' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(t => t.Trim().ToLower())
                                          .Where(t => !string.IsNullOrEmpty(t))
                                          .Distinct()
                                          .ToList();

                var existingTags = await db.Tags.Where(t => tagNames.Contains(t.Name.ToLower())).ToListAsync();

                foreach (var tagName in tagNames)
                {
                    var tag = existingTags.FirstOrDefault(t => t.Name.ToLower() == tagName);
                    if (tag == null)
                    {
                        tag = new Tag { Id = Guid.NewGuid(), Name = tagName };
                        db.Tags.Add(tag);
                    }
                    tagsToAssign.Add(tag);
                }
            }

            var newItem = new MediaItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = string.Empty,
                Status = MediaStatusExtensions.Planned,
                CreatedAt = DateTime.UtcNow,
                Tags = tagsToAssign
            };

            db.MediaItems.Add(newItem);
            await db.SaveChangesAsync();

            Items.Insert(0, newItem);
            NewItemTitle = string.Empty;
            NewItemTags = string.Empty;
            StatusMessage = $"Картку «{title}» успішно збережено!";

            ApplyFilter();
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

    [RelayCommand]
    public void FilterItems(string statusFilter)
    {
        ICollectionView view = CollectionViewSource.GetDefaultView(Items);

        if (string.IsNullOrWhiteSpace(statusFilter) || statusFilter == "Всі")
        {
            view.Filter = null;
        }
        else
        {
            view.Filter = obj =>
            {
                if (obj is MediaItem item)
                {
                    return item.Status == statusFilter;
                }
                return false;
            };
        }
    }

    [RelayCommand]
    public async Task UpdateItemStatusAsync(MediaItem item)
    {
        if (item == null) return;

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var existingItem = await db.MediaItems.FindAsync(item.Id);
            if (existingItem != null)
            {
                existingItem.Status = item.Status;
                await db.SaveChangesAsync();

                StatusMessage = $"Статус успішно змінено на «{item.Status}»";
                ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Помилка оновлення: {ex.InnerException?.Message ?? ex.Message}";
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