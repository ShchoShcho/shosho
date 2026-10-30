using ShoSho.Core.Models;

namespace ShoSho.Core.Services;

public static class ReviewRules
{
    public static bool CanEdit(string status) => status == MediaStatusExtensions.Completed;

    public static void ValidateChange(string status, int? rating, string text, int? previousRating, string previousText)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentException("Оцінка має бути від 1 до 5.");
        if (text.Length > 2000)
            throw new ArgumentException("Відгук має містити до 2000 символів.");
        // Changing a status must not erase impressions recorded earlier.
        if (!CanEdit(status) && (rating != previousRating || text != previousText))
            throw new ArgumentException("Щоб змінити оцінку або відгук, оберіть статус «Вже всьо».");
    }
}
