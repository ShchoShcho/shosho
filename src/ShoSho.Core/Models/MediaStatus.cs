namespace ShoSho.Core.Models;

public enum MediaStatus
{
    Planned,    // В планах
    InProgress, // В процесі
    Completed,  // Завершено
    Postponed   // Відкладено
}

public static class MediaStatusExtensions
{
    public const string Planned = "В планах";
    public const string InProgress = "В процесі";
    public const string Completed = "Завершено";
    public const string Postponed = "Відкладено";

    public static string ToDisplayName(this MediaStatus status) => status switch
    {
        MediaStatus.Planned => Planned,
        MediaStatus.InProgress => InProgress,
        MediaStatus.Completed => Completed,
        MediaStatus.Postponed => Postponed,
        _ => status.ToString()
    };
}

