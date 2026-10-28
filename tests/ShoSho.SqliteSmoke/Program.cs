using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShoSho.Core.Models;
using ShoSho.Infrastructure.Data;

var directory = Path.Combine(Path.GetTempPath(), "ShoSho-test-" + Guid.NewGuid());
var databasePath = Path.Combine(directory, "nested", "test.db");
var options = new DbContextOptionsBuilder<ShoShoDbContext>();
ShoShoDbContext.Configure(options, new SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString());

void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var createdAt = new DateTime(2026, 10, 28, 12, 30, 0, DateTimeKind.Utc);
var id = Guid.NewGuid();
try
{
    await using (var db = new ShoShoDbContext(options.Options))
    {
        Check(!db.Database.HasPendingModelChanges(), "Migration snapshot does not match model.");
        await db.Database.MigrateAsync();
        db.MediaItems.AddRange(
            new MediaItem { Id = id, Title = "Тестова картка 🎬", CreatedAt = createdAt },
            new MediaItem { Title = "Раніше", CreatedAt = createdAt.AddDays(-1) });
        await db.SaveChangesAsync();
    }

    await using (var db = new ShoShoDbContext(options.Options))
    {
        await db.Database.MigrateAsync();
        var items = await db.MediaItems.OrderByDescending(x => x.CreatedAt).ToListAsync();
        Check(items.Count == 2 && items[0].Id == id, "Persistence or date ordering failed.");
        Check(items[0].Title == "Тестова картка 🎬", "Unicode round trip failed.");
        Check(items[0].CreatedAt == createdAt && items[0].CreatedAt.Kind == DateTimeKind.Utc,
            "UTC round trip failed.");
        Check(items[0].PosterUrl is null && items[0].Status == MediaStatusExtensions.Planned,
            "Nullable field or status round trip failed.");
        items[0].Title = "Оновлено";
        await db.SaveChangesAsync();
    }

    await using (var db = new ShoShoDbContext(options.Options))
    {
        var item = await db.MediaItems.SingleAsync(x => x.Id == id);
        Check(item.Title == "Оновлено", "Update failed.");
        db.MediaItems.Remove(item);
        await db.SaveChangesAsync();
        Check(await db.MediaItems.CountAsync() == 1, "Delete failed.");
    }
    Console.WriteLine("PASS: SQLite migrations, reopen, CRUD, Unicode, UTC and ordering.");
}
finally
{
    SqliteConnection.ClearAllPools();
    // Only the uniquely named test directory created above is removed.
    if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
}
