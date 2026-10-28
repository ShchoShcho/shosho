namespace ShoSho.Core.Models;

public class MediaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = MediaStatusExtensions.Planned;
    public string? PosterUrl { get; set; }
    
    public string Type { get; set; } = "Книга";
    public string Tags { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string Review { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}