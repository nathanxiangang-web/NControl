using System.Windows;
using System.Windows.Input;
using NControl.Presentation.ViewModels;

namespace NControl.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        App.Trace("MainWindow ctor begin");
        InitializeComponent();
        DataContext = viewModel;
        PreviewKeyDown += OnPreviewKeyDown;
        App.Trace("MainWindow ctor end");
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is MainViewModel vm && vm.SearchOpen)
        {
            vm.CloseSearchCommand.Execute(null);
            e.Handled = true;
        }
    }
}
