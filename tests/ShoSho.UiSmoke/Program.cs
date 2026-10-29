using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShoSho.Core.Models;
using ShoSho.Infrastructure.Data;
using ShoSho.UI;
using ShoSho.UI.ViewModels;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var directory = Path.Combine(Path.GetTempPath(), "ShoSho-ui-test-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        var options = new DbContextOptionsBuilder<ShoShoDbContext>().UseSqlite($"Data Source={Path.Combine(directory, "test.db")}").Options;
        try
        {
            var factory = new Factory(options);
            using (var db = factory.CreateDbContext())
            {
                db.Database.Migrate();
                db.MediaItems.AddRange(
                    new MediaItem { Title = "Перша", Tags = new List<Tag> { new() { Name = "ЗАТИШОК" } } },
                    new MediaItem { Title = "Друга", Status = MediaStatusExtensions.Completed });
                db.SaveChanges();
            }
            var vm = new MainViewModel(factory);
            Pump(vm.InitializeAsync());
            Check(vm.Items.Count == 2, vm.StatusMessage);
            vm.SearchTagFilter = "затишок";
            var view = CollectionViewSource.GetDefaultView(vm.Items);
            Check(view.Cast<MediaItem>().Single().Title == "Перша", "Tag filtering failed.");
            vm.FilterItemsCommand.Execute(MediaStatusExtensions.Completed);
            Check(view.IsEmpty, "Status filter erased the tag filter.");
            vm.SearchTagFilter = "";
            Check(view.Cast<MediaItem>().Single().Title == "Друга", "Clearing tag erased status filter.");
            vm.FilterItemsCommand.Execute("Всі");
            Check(view.Cast<MediaItem>().Count() == 2, "Reset filter failed.");
            var first = vm.Items.Single(x => x.Title == "Перша");
            first.Status = MediaStatusExtensions.Completed;
            Pump(vm.UpdateItemStatusAsync(first));
            vm.ToggleReviewSectionCommand.Execute(first);
            Check(first.IsReviewSectionOpen && first.Review != null, "Review editor failed.");
            first.Review!.Text = "Чудово";
            Pump(vm.SaveReviewAsync(first));
            first.Review.Rating = 5;
            Pump(vm.SaveReviewAsync(first));
            using (var db = factory.CreateDbContext())
            {
                Check(db.Reviews.Single().Rating == 5, vm.StatusMessage);
                var tags = MainViewModel.ResolveTagsAsync(db, "затишок, ЗАТИШОК, #нове").GetAwaiter().GetResult();
                Check(tags.Count == 2 && tags[0].Id == first.Tags.Single().Id, "Unicode tag reuse failed.");
            }
            vm.SelectedRatingSort = "Від найвищого";
            Check(view.Cast<MediaItem>().First().Id == first.Id, "Rating sorting failed.");
            // The details editor must never bind directly to the live item.
            var load = vm.LoadItemDetailsAsync(first);
            Pump(load);
            var draft = load.Result;
            Check(draft.Title == first.Title && draft.Rating == 5 && draft.ReviewText == "Чудово" &&
                  draft.Tags == "ЗАТИШОК" && draft.MediaType == first.Type && draft.Status == first.Status,
                  "Details did not load all metadata.");
            var detailsWindow = new ItemDetailsWindow(draft);
            detailsWindow.Measure(new Size(820, 760));
            detailsWindow.Arrange(new Rect(0, 0, 820, 760));
            detailsWindow.UpdateLayout();
            Check(((StackPanel)detailsWindow.FindName("ReadPanel")).Visibility == Visibility.Visible,
                "Details must open in read mode.");
            draft.EditCommand.Execute(null);
            draft.Title = "Незбережене";
            draft.ReviewText = "Не записувати";
            detailsWindow.Close();
            Check(first.Title == "Перша" && first.Review!.Text == "Чудово", "Closing mutated the live collection.");
            using (var db = factory.CreateDbContext())
                Check(db.MediaItems.Single(x => x.Id == first.Id).Title == "Перша" && db.Reviews.Single().Text == "Чудово",
                    "Closing details saved changes.");

            load = vm.LoadItemDetailsAsync(first);
            Pump(load);
            draft = load.Result;
            draft.Title = " ";
            Pump(draft.SaveAsync());
            Check(draft.ErrorMessage.Length > 0, "Empty title was accepted.");
            draft.Title = "Оновлена картка";
            draft.MediaType = "Фільм";
            draft.Status = MediaStatusExtensions.InProgress;
            draft.Description = "Повна нотатка";
            draft.Tags = "затишок, новий";
            draft.Rating = 3;
            draft.ReviewText = "Оновлений відгук";
            draft.PosterUrl = "pack://application:,,,/ShoSho.UI;component/Assets/placeholder.jpg";
            var saved = false;
            draft.Saved += () => saved = true;
            vm.SelectedStatusFilter = MediaStatusExtensions.Completed;
            Pump(draft.SaveAsync());
            Check(saved && draft.ErrorMessage.Length == 0, draft.ErrorMessage);
            Check(!view.Cast<MediaItem>().Any(x => x.Id == first.Id), "Saved status did not refresh filters.");
            using (var db = factory.CreateDbContext())
            {
                var stored = db.MediaItems.Include(x => x.Tags).Include(x => x.Review).Single(x => x.Id == first.Id);
                Check(stored.Title == draft.Title && stored.Type == draft.MediaType && stored.Description == draft.Description &&
                      stored.Status == draft.Status && stored.PosterUrl == draft.PosterUrl && stored.Tags.Count == 2 &&
                      stored.Review?.Rating == 3 && stored.Review.Text == draft.ReviewText, "Detail edits were not persisted.");
            }
            // Viewing is available for every status, including an empty wishlist entry.
            foreach (var status in vm.AvailableStatuses)
            {
                using (var db = factory.CreateDbContext())
                {
                    var stored = db.MediaItems.Single(x => x.Id == first.Id);
                    stored.Status = status;
                    db.SaveChanges();
                }
                load = vm.LoadItemDetailsAsync(vm.Items.Single(x => x.Id == first.Id));
                Pump(load);
                Check(load.Result.Status == status, "Details unavailable for status: " + status);
            }
            var failingDraft = new ItemDetailsViewModel(first, null, _ => throw new IOException("Test write failure"));
            Pump(failingDraft.SaveAsync());
            Check(failingDraft.ErrorMessage.Contains("Test write failure") && !failingDraft.IsSaving,
                "Save failure did not leave the draft available for retry.");
            vm.SelectedStatusFilter = "Усі";
            // Construct and lay out the real XAML without showing a desktop window.
            var window = new MainWindow(vm);
            window.Measure(new Size(1100, 760));
            window.Arrange(new Rect(0, 0, 1100, 760));
            window.UpdateLayout();
            var add = new AddItemWindow("Назва", "тег");
            Check(add.ItemTitle == "Назва" && add.Tags == "тег", "Add dialog lost initial fields.");
            ((ComboBox)add.FindName("StatusInput")).SelectedItem = MediaStatusExtensions.Completed;
            Check(((StackPanel)add.FindName("ReviewPanel")).Visibility == Visibility.Visible, "Completed review section hidden.");
            add.Close();
            window.Close();
            Console.WriteLine("PASS: WPF XAML, combined filters, status persistence, review insert/update, rating sort, Ukrainian tag reuse and add dialog.");
            Console.WriteLine("PASS: detail metadata, all statuses, detached edits/cancel, validation, persistence, collection refresh and save failure.");
        }
        finally
        {
            app.Shutdown();
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }

    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Pump(Task task)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (task.IsCompleted) frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
        timer.Stop();
        task.GetAwaiter().GetResult();
    }
    private sealed class Factory(DbContextOptions<ShoShoDbContext> options) : IDbContextFactory<ShoShoDbContext>
    {
        public ShoShoDbContext CreateDbContext() => new(options);
    }
}
