using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ShoSho.UI.ViewModels;

namespace ShoSho.UI;

public partial class ItemDetailsWindow : Window
{
    private readonly ItemDetailsViewModel _viewModel;
    private bool _savedOrDeleted;

    public ItemDetailsWindow(ItemDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.Saved += OnSaved;
        viewModel.Deleted += OnDeleted;
        Closed += (_, _) => 
        { 
            viewModel.Saved -= OnSaved; 
            viewModel.Deleted -= OnDeleted;
        };

        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
    }

    private void OnSaved() { _savedOrDeleted = true; Close(); }
    private void OnDeleted() { _savedOrDeleted = true; Close(); }

    private void CloseClicked(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_viewModel.IsSaving && !_savedOrDeleted) e.Cancel = true;
        base.OnClosing(e);
    }
}