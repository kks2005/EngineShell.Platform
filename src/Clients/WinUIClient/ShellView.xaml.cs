using Microsoft.UI.Xaml;
using Presentation.ViewModels;

namespace WinUIClient;

public sealed partial class ShellView : Window
{
    public ShellView(ShellViewModel shellViewModel)
    {
        InitializeComponent();
        Root.DataContext = shellViewModel;
    }
}
