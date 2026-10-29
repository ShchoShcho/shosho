using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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

    foreach (var baseline in new[] { "20261008185827_InitialCreate", "20261028223030_UpdateTagsStructure", "20261029105641_InitialCreate", "20261029120845_AddReviewAndPropertiess" })
    {
        var upgradeOptions = new DbContextOptionsBuilder<ShoShoDbContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, baseline + ".db")}").Options;
        await using var upgrade = new ShoShoDbContext(upgradeOptions);
        await upgrade.GetService<IMigrator>().MigrateAsync(baseline);
        if (baseline == "20261029105641_InitialCreate")
        {
            // A database made on the tracking branch had only its replacement initial migration.
            await upgrade.Database.ExecuteSqlRawAsync("DELETE FROM __EFMigrationsHistory WHERE MigrationId <> '20261029105641_InitialCreate';");
        }
        var oldId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        await upgrade.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO MediaItems (Id, Title, Description, Status, CreatedAt)
            VALUES ({oldId}, {"Старий запис"}, {"Нотатка"}, {"Завершено"}, {createdAt});
            """);
        if (baseline.Contains("UpdateTagsStructure"))
        {
            await upgrade.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Tag (Id, Name) VALUES ({tagId}, {"затишок"});");
            await upgrade.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO MediaItemTag (MediaItemsId, TagsId) VALUES ({oldId}, {tagId});");
        }
        if (baseline.Contains("Propertiess"))
        {
            await upgrade.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Tags (Id, Name) VALUES ({tagId}, {"затишок"});");
            // Simulate the branch's already-applied legacy table layout.
            await upgrade.Database.ExecuteSqlRawAsync("CREATE TABLE MediaItemTag (MediaItemsId TEXT NOT NULL, TagsId TEXT NOT NULL);");
            await upgrade.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO MediaItemTag (MediaItemsId, TagsId) VALUES ({oldId}, {tagId});");
            await upgrade.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Reviews (Id, MediaItemId, Rating, Text) VALUES ({Guid.NewGuid()}, {oldId}, {4}, {"Старий відгук"});");
        }
        await upgrade.Database.MigrateAsync();
        await upgrade.Database.MigrateAsync();
        var preserved = await upgrade.MediaItems.Include(x => x.Tags).Include(x => x.Review).SingleAsync();
        Check(preserved.Id == oldId && preserved.Title == "Старий запис" && preserved.Description == "Нотатка", "Upgrade lost media data.");
        Check(preserved.Status == MediaStatusExtensions.Completed && preserved.Type == "Книга", "Legacy status/type not upgraded.");
        if (!baseline.Contains("InitialCreate"))
            Check(preserved.Tags.Single().Name == "затишок", "Upgrade lost tag assignments.");
        if (baseline.Contains("Propertiess"))
            Check(preserved.Review?.Text == "Старий відгук" && preserved.Review.Rating == 4, "Upgrade lost review.");
        else
        {
            preserved.Review = new Review { MediaItemId = oldId, Text = "Без оцінки", Rating = null };
            upgrade.Reviews.Add(preserved.Review);
            await upgrade.SaveChangesAsync();
        }
        upgrade.MediaItems.Remove(preserved);
        await upgrade.SaveChangesAsync();
        Check(!await upgrade.Reviews.AnyAsync(), "Review cascade delete failed.");
    }
    Console.WriteLine("PASS: upgrades from original SQLite, legacy tags and reviews preserve data; nullable ratings and cascade delete.");
}
finally
{
    SqliteConnection.ClearAllPools();
    // Only the uniquely named test directory created above is removed.
    if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
}
