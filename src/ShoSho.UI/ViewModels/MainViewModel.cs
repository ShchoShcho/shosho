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

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _visibleCount;
    [ObservableProperty] private string _tagButtonText = "Теги";
    [ObservableProperty] private CategoryFilter? _selectedCategory;

    public ObservableCollection<CategoryFilter> Categories { get; } = new();
    public ObservableCollection<TagOption> AvailableTags { get; } = new();
    private static readonly (string Name, string Icon, string Type)[] BuiltIn =
    {
        ("Фільми",  "🎬", "Фільм"),
        ("Серіали", "📺", "Серіал"),
        ("Книги",   "📚", "Книга"),
        ("Ігри",    "🎮", "Гра"),
        ("Інше",    "✨", "Інше"),
    };

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryChanged(CategoryFilter? oldValue, CategoryFilter? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
        ApplyFilter();
    }

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
        BuildCategories();
    }
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

        var search = SearchText.Trim();
        var category = SelectedCategory;
        var status = SelectedStatusFilter;
        var selectedTags = AvailableTags.Where(t => t.IsSelected).Select(t => t.Name).ToList();
        bool anyStatus = string.IsNullOrEmpty(status) || status == "Усі" || status == "Усі статуси";

        view.Filter = obj =>
        {
            if (obj is not MediaItem item) return false;

            if (category != null && !category.Matches(item.Type?.ToString())) return false;
            if (!anyStatus && item.Status != status) return false;
            if (search.Length > 0 &&
                (item.Title ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return selectedTags.All(sel =>
                item.Tags.Any(t => string.Equals(t.Name, sel, StringComparison.OrdinalIgnoreCase)));
        };

        VisibleCount = view.Cast<object>().Count();
    }

    // ---------- Категорії ----------
    private void BuildCategories()
    {
        Categories.Clear();
        Categories.Add(new CategoryFilter("Усі враження", "🗂", null));
        foreach (var (name, icon, type) in BuiltIn)
            Categories.Add(new CategoryFilter(name, icon, type));
        foreach (var c in CategoryStore.Load())
            Categories.Add(new CategoryFilter(c.Name, c.Icon, c.Name, isCustom: true));

        SelectedCategory = Categories[0];
    }
    private void RefreshCategoryCounts()
    {
        foreach (var c in Categories)
            c.Count = Items.Count(i => c.Matches(i.Type?.ToString()));
    }

    [RelayCommand]
    private void SelectCategory(CategoryFilter? category)
    {
        if (category != null) SelectedCategory = category;
    }

    [RelayCommand]
    private void CreateCategory()
    {
        var taken = Categories.Select(c => c.Name).Concat(Categories.Where(c => c.TypeValue != null).Select(c => c.TypeValue!));
        var window = new CreateCategoryWindow(taken);
        if (Application.Current?.MainWindow is Window owner) window.Owner = owner;
        if (window.ShowDialog() != true) return;

        var category = new CategoryFilter(window.CategoryName, window.CategoryIcon,
            window.CategoryName, isCustom: true);
        Categories.Add(category);

        try
        {
            CategoryStore.Save(Categories.Where(c => c.IsCustom)
                .Select(c => new CustomCategory(c.Name, c.Icon)));
            StatusMessage = $"Категорію «{category.Name}» створено.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Категорію створено, але не збережено: {ex.Message}";
        }

        RefreshCategoryCounts();
        SelectedCategory = category;
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(CategoryFilter? category)
    {
        if (category is not { IsCustom: true }) return;

        var affectedItems = Items.Where(i => category.Matches(i.Type)).ToList();

        var text = affectedItems.Count > 0
            ? $"Видалити категорію «{category.Name}»?\nЗаписів: {affectedItems.Count}. Вони будуть переміщені до категорії «Інше»."
            : $"Видалити категорію «{category.Name}»?";

        if (MessageBox.Show(text, "Видалення категорії",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            if (affectedItems.Count > 0)
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var stored = (await db.MediaItems.ToListAsync())
                    .Where(m => category.Matches(m.Type));
                foreach (var m in stored) m.Type = "Інше";
                await db.SaveChangesAsync();

                // оновлюємо картки в UI
                foreach (var item in affectedItems)
                {
                    item.Type = "Інше";
                    var index = Items.IndexOf(item);
                    if (index >= 0) Items[index] = item;
                }
            }

            if (SelectedCategory == category)
                SelectedCategory = Categories[0];

            Categories.Remove(category);
            CategoryStore.Save(Categories.Where(c => c.IsCustom)
                .Select(c => new CustomCategory(c.Name, c.Icon)));

            RefreshCategoryCounts();
            RefreshTagOptions();
            StatusMessage = $"Категорію «{category.Name}» видалено.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не вдалося видалити категорію: {ex.Message}";
        }
    }

    // ---------- Теги ----------
    private void RefreshTagOptions()
    {
        var selected = AvailableTags.Where(t => t.IsSelected).Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var t in AvailableTags) t.PropertyChanged -= OnTagOptionChanged;
        AvailableTags.Clear();

        var names = Items.SelectMany(i => i.Tags).Select(t => t.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase);

        foreach (var name in names)
        {
            var option = new TagOption(name) { IsSelected = selected.Contains(name) };
            option.PropertyChanged += OnTagOptionChanged;
            AvailableTags.Add(option);
        }

        UpdateTagSummary();
        ApplyFilter();
    }

    private void OnTagOptionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TagOption.IsSelected)) return;
        UpdateTagSummary();
        ApplyFilter();
    }

    private void UpdateTagSummary()
    {
        var n = AvailableTags.Count(t => t.IsSelected);
        TagButtonText = n == 0 ? "Теги" : $"Теги: {n}";
    }

    [RelayCommand]
    private void ClearTags()
    {
        foreach (var t in AvailableTags) t.IsSelected = false;
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
            RefreshCategoryCounts();
            RefreshTagOptions();
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
        var addWindow = new AddItemWindow(
            types: Categories.Where(c => c.TypeValue != null).Select(c => c.TypeValue!),
            selectedType: SelectedCategory?.TypeValue);
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
                RefreshCategoryCounts();
                RefreshTagOptions();
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

        return new ItemDetailsViewModel(
            stored,
            item.PosterUrl,
            SaveItemDetailsAsync,
            delete: async draft =>
            {
                await using var deleteDb = await _dbContextFactory.CreateDbContextAsync();
                var dbItem = await deleteDb.MediaItems.FindAsync(draft.Id);
                if (dbItem != null)
                {
                    deleteDb.MediaItems.Remove(dbItem);
                    await deleteDb.SaveChangesAsync();
                }

                var target = Items.FirstOrDefault(x => x.Id == draft.Id);
                if (target != null)
                {
                    Items.Remove(target);
                }
                RefreshCategoryCounts();
                RefreshTagOptions();
                ApplySorting();
                StatusMessage = $"Картку «{draft.Title}» успішно видалено.";
            }
        );
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
        RefreshCategoryCounts();
        RefreshTagOptions();
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
                db.Reviews.Add(new Review
                {
                    MediaItemId = item.Id,
                    Rating = item.Review.Rating,
                    Text = item.Review.Text
                });
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
