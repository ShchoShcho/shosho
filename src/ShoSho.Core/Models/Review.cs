using System;

namespace ShoSho.Core.Models;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int? Rating { get; set; }

    public string Text { get; set; } = string.Empty;

    public Guid MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
}