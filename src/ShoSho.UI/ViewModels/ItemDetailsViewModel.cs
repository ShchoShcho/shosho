using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShoSho.Core.Models;
using ShoSho.Core.Services;
using System.Windows;

namespace ShoSho.UI.ViewModels;

public partial class ItemDetailsViewModel : ObservableObject
{
    private readonly Func<ItemDetailsViewModel, Task> _save;
    private readonly Func<ItemDetailsViewModel, Task>? _delete;
    private readonly int? _originalRating;
    private readonly string _originalReviewText;
    public bool CanEditReview => ReviewRules.CanEdit(Status);
    
    public Guid Id { get; }
    public string? CoverSource { get; }
    public string[] MediaTypes { get; }
    public string[] Statuses { get; }
    public int?[] Ratings { get; } = { null, 1, 2, 3, 4, 5 };

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _mediaType = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditReview))]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _posterUrl = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _tags = string.Empty;

    [ObservableProperty]
    private int? _rating;

    [ObservableProperty]
    private string _reviewText = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event Action? Saved;
    public event Action? Deleted;

    public ItemDetailsViewModel(
        MediaItem item, 
        string? coverSource, 
        Func<ItemDetailsViewModel, Task> save, 
        Func<ItemDetailsViewModel, Task>? delete = null)
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
        _originalRating = _rating;
        _originalReviewText = _reviewText;
        MediaTypes = new[] { "Книга", "Фільм", "Серіал", "Гра", "Інше", item.Type }.Distinct().ToArray();
        Statuses = Enum.GetValues<MediaStatus>().Select(s => s.ToDisplayName()).Append(item.Status).Distinct().ToArray();
        _save = save;
        _delete = delete;
    }

    [RelayCommand]
    private void Edit() => IsEditing = true;

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (_delete == null || IsSaving) return;

        var result = MessageBox.Show(
            $"Ви дійсно бажаєте видалити запис «{Title}»?",
            "Підтвердження видалення",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            IsSaving = true;
            ErrorMessage = string.Empty;
            
            await _delete(this);
            
            Deleted?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не вдалося видалити: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally 
        { 
            IsSaving = false; 
        }
    }
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
            ReviewRules.ValidateChange(Status, Rating, ReviewText, _originalRating, _originalReviewText);
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
