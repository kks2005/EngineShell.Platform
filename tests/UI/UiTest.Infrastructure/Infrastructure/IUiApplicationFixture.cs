using FlaUI.Core.AutomationElements;

namespace UiTest.Infrastructure;

public interface IUiApplicationFixture : IDisposable
{
    Window MainWindow { get; }
}
