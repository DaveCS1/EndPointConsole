using System.Windows;
using EndpointConsole.Wpf.ViewModels;

namespace EndpointConsole.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
