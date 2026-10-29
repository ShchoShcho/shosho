using System.Windows;
using System.Windows.Controls;
using ShoSho.Core.Models;
using ShoSho.UI.ViewModels;

namespace ShoSho.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }

    private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.DataContext is MediaItem item)
        {
            if (_viewModel.UpdateItemStatusCommand.CanExecute(item))
            {
                _viewModel.UpdateItemStatusCommand.Execute(item);
            }
        }
    }
    private void CloseTagPopup_Click(object sender, RoutedEventArgs e) => TagToggle.IsChecked = false;
}