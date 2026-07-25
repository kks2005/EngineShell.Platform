using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Presentation.ViewModels;

namespace WinUIClient;

public sealed class ViewModelTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GeneralTemplate { get; set; }

    public DataTemplate? ScreensTemplate { get; set; }

    public DataTemplate? RenderEngineTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        ResolveTemplate(item);

    protected override DataTemplate? SelectTemplateCore(
        object item,
        DependencyObject container) =>
        ResolveTemplate(item);

    private DataTemplate? ResolveTemplate(object item)
    {
        return item switch
        {
            GeneralViewModel => GeneralTemplate,
            ScreensViewModel => ScreensTemplate,
            RenderEngineViewModel => RenderEngineTemplate,
            _ => null
        };
    }
}
