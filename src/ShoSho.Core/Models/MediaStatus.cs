namespace ShoSho.Core.Models;

public enum MediaStatus
{
    Planned,
    InProgress,
    Completed,
    Postponed
}

public static class MediaStatusExtensions
{
    public const string Planned = "Хочу";
    public const string InProgress = "Ін прогрес";
    public const string Completed = "Вже всьо";
    public const string Postponed = "Вагаюсі";

    public static string ToDisplayName(this MediaStatus status) => status switch
    {
        MediaStatus.Planned => Planned,
        MediaStatus.InProgress => InProgress,
        MediaStatus.Completed => Completed,
        MediaStatus.Postponed => Postponed,
        _ => status.ToString()
    };
}