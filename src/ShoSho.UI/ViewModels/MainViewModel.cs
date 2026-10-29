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

namespace ShoSho.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDbContextFactory<ShoShoDbContext> _dbContextFactory;


    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchTagFilter = string.Empty;

    [ObservableProperty]
    private string _selectedStatusFilter = "Усі";

    public ObservableCollection<MediaItem> Items { get; } = new();

    public List<string> AvailableStatuses { get; } = new()
    {
        MediaStatusExtensions.Planned,
        MediaStatusExtensions.InProgress,
        MediaStatusExtensions.Completed,
        MediaStatusExtensions.Postponed
    };

    public string[] RatingSortOptions { get; } = { "За датою", "Від найвищого", "Від найнижчого" };
    [ObservableProperty]
    private string _selectedRatingSort = "За датою";
    partial void OnSelectedRatingSortChanged(string value) => ApplySorting();
    private void ApplySorting()
    {
        var view = CollectionViewSource.GetDefaultView(Items);
        using (view.DeferRefresh())
        {
            view.SortDescriptions.Clear();
            if (SelectedRatingSort != "За датою")
                view.SortDescriptions.Add(new SortDescription("Review.Rating",
                    SelectedRatingSort == "Від найвищого" ? ListSortDirection.Descending : ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription(nameof(MediaItem.CreatedAt), ListSortDirection.Descending));
        }
    }

    public IEnumerable<string> StatusFilters => new[] { "Усі" }.Concat(AvailableStatuses);

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

    // Відстеження змін фільтрів

    partial void OnSearchTagFilterChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedStatusFilterChanged(string value)
    {
        ApplyFilter();
    }


    // Фільтрація карток

    private void ApplyFilter()
    {
        var view = CollectionViewSource.GetDefaultView(Items);

        if (string.IsNullOrWhiteSpace(SearchTagFilter) &&
            (string.IsNullOrEmpty(SelectedStatusFilter) ||
             SelectedStatusFilter == "Усі" ||
             SelectedStatusFilter == "Усі статуси"))
        {
            view.Filter = null;
            return;
        }

        view.Filter = obj =>
        {
            if (obj is not MediaItem item)
                return false;

            bool matchesStatus =
                string.IsNullOrEmpty(SelectedStatusFilter) ||
                SelectedStatusFilter == "Усі" ||
                SelectedStatusFilter == "Усі статуси" ||
                item.Status == SelectedStatusFilter;

            bool matchesTag = true;

            if (!string.IsNullOrWhiteSpace(SearchTagFilter))
            {
                matchesTag = item.Tags.Any(t =>
                    t.Name.Contains(SearchTagFilter.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return matchesStatus && matchesTag;
        };
    }


    // Завантаження даних

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
                .Include(m => m.Review)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            Items.Clear();

            foreach (var item in items)
            {
                await CachePosterAsync(item);
                Items.Add(item);
            }

            ApplySorting();
            StatusMessage = $"Завантажено карток: {Items.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Помилка підключення до БД: {ex.Message}";
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
        if (Application.Current?.MainWindow is Window owner)
            addWindow.Owner = owner;

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

                    CreatedAt = DateTime.UtcNow
                };

                await using var db = await _dbContextFactory.CreateDbContextAsync();
                newItem.Tags = await ResolveTagsAsync(db, addWindow.Tags);
                if (newItem.Status == MediaStatusExtensions.Completed)
                    newItem.Review = new Review { MediaItemId = newItem.Id, Rating = addWindow.Rating, Text = addWindow.Review };
                db.MediaItems.Add(newItem);
                await db.SaveChangesAsync();

                await CachePosterAsync(newItem);
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
        SelectedStatusFilter = statusFilter == "Всі" ? "Усі" : statusFilter;
    }

    [RelayCommand]
    private async Task OpenItemDetailsAsync(MediaItem item)
    {
        try
        {
            var details = await LoadItemDetailsAsync(item);
            var window = new ItemDetailsWindow(details);
            if (Application.Current?.MainWindow is Window owner) window.Owner = owner;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не вдалося відкрити запис: {ex.Message}";
        }
    }

    public async Task<ItemDetailsViewModel> LoadItemDetailsAsync(MediaItem item)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var stored = await db.MediaItems.AsNoTracking().Include(x => x.Tags).Include(x => x.Review)
            .SingleAsync(x => x.Id == item.Id);
        return new ItemDetailsViewModel(stored, item.PosterUrl, SaveItemDetailsAsync);
    }

    private async Task SaveItemDetailsAsync(ItemDetailsViewModel draft)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var stored = await db.MediaItems.Include(x => x.Tags).Include(x => x.Review)
            .SingleAsync(x => x.Id == draft.Id);
        var tags = await ResolveTagsAsync(db, draft.Tags);
        stored.Title = draft.Title.Trim();
        stored.Type = draft.MediaType;
        stored.Status = draft.Status;
        stored.PosterUrl = string.IsNullOrWhiteSpace(draft.PosterUrl) ? null : draft.PosterUrl.Trim();
        stored.Description = draft.Description;
        stored.Tags.Clear();
        foreach (var tag in tags) stored.Tags.Add(tag);
        stored.Rating = draft.Rating;
        if (stored.Review != null)
        {
            stored.Review.Rating = draft.Rating;
            stored.Review.Text = draft.ReviewText;
        }
        else if (draft.Rating != null || !string.IsNullOrWhiteSpace(draft.ReviewText))
        {
            stored.Review = new Review { MediaItemId = stored.Id, Rating = draft.Rating, Text = draft.ReviewText };
            db.Reviews.Add(stored.Review);
        }
        await db.SaveChangesAsync();
        await CachePosterAsync(stored);
        var previous = Items.FirstOrDefault(x => x.Id == stored.Id);
        if (previous != null) Items[Items.IndexOf(previous)] = stored;
        ApplyFilter();
        ApplySorting();
        StatusMessage = $"Зміни до «{stored.Title}» збережено.";
    }

    // Оновлення статусу картки

    [RelayCommand]
    public async Task UpdateItemStatusAsync(MediaItem item)
    {
        if (item == null)
            return;

        try
        {
            await using var db =
                await _dbContextFactory.CreateDbContextAsync();

            var existingItem =
                await db.MediaItems.FindAsync(item.Id);

            if (existingItem != null)
            {
                // Захист від нескінченного циклу оновлення WPF
                if (existingItem.Status == item.Status)
                {
                    if (item.Status == MediaStatusExtensions.Completed &&
                        item.Review == null)
                    {
                        item.Review = new Review
                        {
                            MediaItemId = item.Id
                        };
                    }

                    return;
                }

                existingItem.Status = item.Status;

                await db.SaveChangesAsync();

                if (item.Status == MediaStatusExtensions.Completed &&
                    item.Review == null)
                {
                    item.Review = new Review
                    {
                        MediaItemId = item.Id
                    };
                }

                StatusMessage =
                    $"Статус успішно змінено на «{item.Status}»";

                ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Помилка оновлення: {ex.InnerException?.Message ?? ex.Message}";
        }
    }


    // Збереження відгуку

    [RelayCommand]
    public async Task SaveReviewAsync(MediaItem item)
    {
        if (item == null || item.Review == null)
            return;

        if (item.Review.Rating is < 1 or > 5 || item.Review.Text.Length > 2000)
        {
            StatusMessage = "Оцінка має бути від 1 до 5, відгук — до 2000 символів.";
            return;
        }

        try
        {
            await using var db =
                await _dbContextFactory.CreateDbContextAsync();

            var existingReview = await db.Reviews
                .FirstOrDefaultAsync(r => r.MediaItemId == item.Id);

            if (existingReview != null)
            {
                existingReview.Rating = item.Review.Rating;
                existingReview.Text = item.Review.Text;
            }
            else
            {
                db.Reviews.Add(new Review { MediaItemId = item.Id,
                    Rating = item.Review.Rating, Text = item.Review.Text });
            }

            await db.SaveChangesAsync();

            ApplySorting();
            StatusMessage =
                $"Відгук для «{item.Title}» збережено!";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Помилка збереження відгуку: {ex.InnerException?.Message ?? ex.Message}";
        }
    }


    [RelayCommand]
    private void ToggleReviewSection(MediaItem item)
    {
        if (item.Status != MediaStatusExtensions.Completed) return;
        item.Review ??= new Review { MediaItemId = item.Id };
        item.IsReviewSectionOpen = !item.IsReviewSectionOpen;
    }

    private static async Task CachePosterAsync(MediaItem item)
    {
        if (Uri.TryCreate(item.PosterUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "http" || uri.Scheme == "https"))
            item.PosterUrl = await ImageCacheService.GetLocalPosterAsync(item.PosterUrl!) ?? item.PosterUrl;
    }

    public static async Task<List<Tag>> ResolveTagsAsync(ShoShoDbContext db, string text)
    {
        var names = text.Split(new[] { ',', '#' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct().ToList();
        if (names.Any(t => t.Length > 50)) throw new ArgumentException("Тег має містити до 50 символів.");
        // SQLite lower() does not fold Ukrainian letters; compare names in .NET.
        var existing = await db.Tags.ToListAsync();
        return names.Select(name =>
        {
            var tag = existing.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (tag != null) return tag;
            tag = new Tag { Name = name };
            db.Tags.Add(tag);
            return tag;
        }).ToList();
    }

    // Фабрика контексту для дизайнера

    private sealed class DesignTimeDbContextFactory
        : IDbContextFactory<ShoShoDbContext>
    {
        public ShoShoDbContext CreateDbContext()
        {
            return new ShoShoDbContext();
        }
    }
}
