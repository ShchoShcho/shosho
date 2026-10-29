using System;

namespace ShoSho.Core.Models;

public class Review : System.ComponentModel.INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();

    private int? _rating;
    public int? Rating
    {
        get => _rating;
        set { _rating = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Rating))); }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public string Text { get; set; } = string.Empty;

    public Guid MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
}
