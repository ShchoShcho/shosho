using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShoSho.UI;

public partial class CreateCategoryWindow : Window
{
    private readonly HashSet<string> _existing;

    public string CategoryName { get; private set; } = string.Empty;
    public string CategoryIcon { get; private set; } = "🏷";

    public CreateCategoryWindow(IEnumerable<string> existingNames)
    {
        InitializeComponent();
        _existing = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        Loaded += (_, _) => NameBox.Focus();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();

        if (name.Length == 0)
        {
            ShowError("Введіть назву категорії.");
            return;
        }
        if (_existing.Contains(name))
        {
            ShowError("Категорія з такою назвою вже існує.");
            return;
        }

        CategoryName = name;
        var icon = IconBox.Text.Trim();
        CategoryIcon = icon.Length == 0 ? "🏷" : icon;
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}