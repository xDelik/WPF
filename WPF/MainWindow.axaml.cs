using Avalonia.Controls;
using WPF.ViewModels;

namespace WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
