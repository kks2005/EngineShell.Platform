using CommunityToolkit.Mvvm.ComponentModel;

namespace Presentation.ViewModels;

public partial class FooterViewModel: ViewModelBase
{
    [ObservableProperty]
    private string? status;
    [ObservableProperty]
    private int progress;
}

