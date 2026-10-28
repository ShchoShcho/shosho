using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
    private string _newItemTags = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<MediaItem> Items { get; } = new();

    public MainViewModel(IDbContextFactory<ShoShoDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

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

            await db.Database.MigrateAsync();

            var items = await db.MediaItems
                .Include(m => m.Tags)
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
            return new ShoShoDbContext();
        }
    }
}