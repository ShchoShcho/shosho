using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShoSho.Core.Models;

namespace ShoSho.UI;

public partial class AddItemWindow : Window
{
    public string ItemTitle => TitleInput.Text.Trim();
    public string MediaType => (string)TypeInput.SelectedItem;
    public string Status => (string)StatusInput.SelectedItem;
    public string? PosterUrl => string.IsNullOrWhiteSpace(PosterInput.Text) ? null : PosterInput.Text.Trim();
    public string Tags => TagsInput.Text;
    public string Note => NoteInput.Text.Trim();
    public int? Rating => int.TryParse(RatingInput.Text, out var rating) ? rating : null;
    public string Review => ReviewInput.Text.Trim();

    public AddItemWindow(string title = "", string tags = "",
        IEnumerable<string>? types = null, string? selectedType = null)
    {
        InitializeComponent();
        TitleInput.Text = title;
        TagsInput.Text = tags;

        var typeList = (types ?? new[] { "Книга", "Фільм", "Серіал", "Гра", "Інше" }).ToList();
        TypeInput.ItemsSource = typeList;
        TypeInput.SelectedItem = selectedType != null && typeList.Contains(selectedType)
            ? selectedType
            : typeList[0];

        StatusInput.ItemsSource = Enum.GetValues<MediaStatus>().Select(s => s.ToDisplayName());
        StatusInput.SelectedIndex = 0;

        ValidateTitle();
    }

    private void StatusChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReviewPanel != null)
            ReviewPanel.Visibility = Status == MediaStatusExtensions.Completed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TitleInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateTitle();
    }

    private bool ValidateTitle()
    {
        bool isEmpty = string.IsNullOrWhiteSpace(ItemTitle);

        if (isEmpty)
        {
            if (TitleErrorText != null) TitleErrorText.Visibility = Visibility.Visible;
            TitleInput.BorderBrush = Brushes.IndianRed;
            if (SaveButton != null) SaveButton.IsEnabled = false;
            return false;
        }
        else
        {
            if (TitleErrorText != null) TitleErrorText.Visibility = Visibility.Collapsed;
            TitleInput.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xD6, 0xC8));
            if (SaveButton != null) SaveButton.IsEnabled = true;
            return true;
        }
    }

    private void SaveClicked(object sender, RoutedEventArgs e)
    {
        if (!ValidateTitle())
        {
            ErrorText.Text = "Поле «Назва» є обов'язковим.";
            return;
        }
        if (Status == MediaStatusExtensions.Completed && !string.IsNullOrWhiteSpace(RatingInput.Text) &&
            (Rating is null or < 1 or > 5))
        {
            ErrorText.Text = "Оцінка має бути цілим числом від 1 до 5.";
            return;
        }
        if (Tags.Split(new[] { ',', '#' }).Any(t => t.Trim().Length > 50))
        {
            ErrorText.Text = "Тег має містити до 50 символів.";
            return;
        }
        DialogResult = true;
    }
}
