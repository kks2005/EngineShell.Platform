namespace Clients.Maui;

public partial class MainPage : ContentPage
{
	public MainPage(Presentation.ViewModels.ShellViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
