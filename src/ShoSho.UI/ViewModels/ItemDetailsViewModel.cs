using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShoSho.Core.Models;

namespace ShoSho.UI.ViewModels;

// A detached draft: closing the window never changes the collection or database.
public partial class ItemDetailsViewModel : ObservableObject
{
    private readonly Func<ItemDetailsViewModel, Task> _save;
    public Guid Id { get; }
    public string? CoverSource { get; }
    public string[] MediaTypes { get; }
    public string[] Statuses { get; }
    public int?[] Ratings { get; } = { null, 1, 2, 3, 4, 5 };
    public event Action? Saved;

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _mediaType;
    [ObservableProperty] private string _status;
    [ObservableProperty] private string _posterUrl;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string _tags;
    [ObservableProperty] private int? _rating;
    [ObservableProperty] private string _reviewText;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public ItemDetailsViewModel(MediaItem item, string? coverSource, Func<ItemDetailsViewModel, Task> save)
    {
        Id = item.Id;
        CoverSource = coverSource;
        _title = item.Title;
        _mediaType = item.Type;
        _status = item.Status;
        _posterUrl = item.PosterUrl ?? string.Empty;
        _description = item.Description;
        _tags = string.Join(", ", item.Tags.Select(t => t.Name));
        _rating = item.Review?.Rating ?? item.Rating;
        _reviewText = item.Review?.Text ?? string.Empty;
        MediaTypes = new[] { "Книга", "Фільм", "Серіал", "Гра", "Інше", item.Type }.Distinct().ToArray();
        Statuses = Enum.GetValues<MediaStatus>().Select(s => s.ToDisplayName()).Append(item.Status).Distinct().ToArray();
        _save = save;
    }

    [RelayCommand]
    private void Edit() => IsEditing = true;

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (IsSaving) return;
        if (string.IsNullOrWhiteSpace(Title) || Title.Trim().Length > 255)
        { ErrorMessage = "Введіть назву довжиною до 255 символів."; return; }
        if (Description.Length > 2000 || ReviewText.Length > 2000 || PosterUrl.Length > 1000)
        { ErrorMessage = "Нотатка та відгук — до 2000 символів, адреса обкладинки — до 1000."; return; }
        if (Rating is < 1 or > 5)
        { ErrorMessage = "Оцінка має бути від 1 до 5."; return; }
        if (Tags.Split(new[] { ',', '#' }).Any(t => t.Trim().Length > 50))
        { ErrorMessage = "Тег має містити до 50 символів."; return; }
        try
        {
            IsSaving = true;
            ErrorMessage = string.Empty;
            await _save(this);
            Saved?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не вдалося зберегти: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally { IsSaving = false; }
    }
}
