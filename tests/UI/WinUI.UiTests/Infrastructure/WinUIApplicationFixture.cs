using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using UiTest.Infrastructure;

namespace WinUI.UiTests.Infrastructure;

public sealed class WinUIApplicationFixture : IUiApplicationFixture
{
    private const string ExecutableEnvironmentVariable = "WINUI_CLIENT_EXE";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(20);

    private WinUIApplicationFixture(
        Application application,
        UIA3Automation automation,
        Window mainWindow)
    {
        Application = application;
        Automation = automation;
        MainWindow = mainWindow;
    }

    public Application Application { get; }
    public UIA3Automation Automation { get; }
    public Window MainWindow { get; }

    public static WinUIApplicationFixture Launch()
    {
        var application = Application.Launch(ResolveExecutablePath());
        var automation = new UIA3Automation();
        try
        {
            var mainWindow = application.GetMainWindow(automation, StartupTimeout)
                ?? throw new InvalidOperationException(
                    $"The WinUI main window did not appear within {StartupTimeout}.");
            return new WinUIApplicationFixture(application, automation, mainWindow);
        }
        catch
        {
            automation.Dispose();
            if (!application.HasExited) application.Kill();
            application.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        Automation.Dispose();
        if (!Application.HasExited) Application.Close(killIfCloseFails: true);
        Application.Dispose();
    }

    private static string ResolveExecutablePath()
    {
        var configuredPath =
            Environment.GetEnvironmentVariable(ExecutableEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullPath = Path.GetFullPath(configuredPath);
            return File.Exists(fullPath)
                ? fullPath
                : throw new FileNotFoundException(
                    $"{ExecutableEnvironmentVariable} points to a missing file.",
                    fullPath);
        }

        var root = FindSolutionRoot(AppContext.BaseDirectory);
        var configuration = AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";
        var executablePath = Path.Combine(
            root, "src", "Clients", "WinUIClient", "bin", configuration,
            "net9.0-windows10.0.19041.0", "win-x64", "WinUIClient.exe");

        return File.Exists(executablePath)
            ? executablePath
            : throw new FileNotFoundException(
                "Build WinUIClient first or set WINUI_CLIENT_EXE.",
                executablePath);
    }

    private static string FindSolutionRoot(string startingDirectory)
    {
        for (var directory = new DirectoryInfo(startingDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClientAgnostic.sln")))
                return directory.FullName;
        }
        throw new DirectoryNotFoundException("Could not locate the solution root.");
    }
}
