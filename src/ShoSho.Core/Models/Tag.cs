namespace ShoSho.Core.Models;

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ICollection<MediaItem> MediaItems { get; set; } = new List<MediaItem>();
}