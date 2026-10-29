using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace ShoSho.Core.Models;

public class MediaItem : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    private string _status = MediaStatusExtensions.Planned;
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
    public string? PosterUrl { get; set; }
    
    public string Type { get; set; } = "Книга";
    public int? Rating { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    private Review? _review;
    public Review? Review { get => _review; set { _review = value; OnPropertyChanged(); } }

    private bool _isReviewSectionOpen;
    [NotMapped]
    public bool IsReviewSectionOpen { get => _isReviewSectionOpen; set { _isReviewSectionOpen = value; OnPropertyChanged(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
}