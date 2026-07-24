using Presentation.ViewModels;
using System.Windows;

namespace WpfClient;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class ShellView : Window
{
    public ShellView(ShellViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}