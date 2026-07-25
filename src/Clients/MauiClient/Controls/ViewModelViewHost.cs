using Clients.Maui.Views;
using Presentation.ViewModels;

namespace Clients.Maui.Controls;

/// <summary>
/// Maps shared view models to their small, platform-specific MAUI views.
/// </summary>
public sealed class ViewModelViewHost : ContentView
{
    public static readonly BindableProperty ViewModelProperty = BindableProperty.Create(
        nameof(ViewModel),
        typeof(object),
        typeof(ViewModelViewHost),
        propertyChanged: OnViewModelChanged);

    public object? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var host = (ViewModelViewHost)bindable;
        host.Content = newValue switch
        {
            GeneralViewModel => new GeneralView(),
            ScreensViewModel => new ScreensView(),
            RenderEngineViewModel => new RenderEngineView(),
            _ => null
        };

        if (host.Content is not null)
        {
            host.Content.BindingContext = newValue;
        }
    }
}
