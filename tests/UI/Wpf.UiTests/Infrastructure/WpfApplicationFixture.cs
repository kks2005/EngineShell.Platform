using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using UiTest.Infrastructure;

namespace Wpf.UiTests.Infrastructure;

public sealed class WpfApplicationFixture : IUiApplicationFixture
{
    private const string ExecutableEnvironmentVariable = "WPF_CLIENT_EXE";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);

    private WpfApplicationFixture(
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

    public static WpfApplicationFixture Launch()
    {
        var executablePath = ResolveExecutablePath();
        var application = Application.Launch(executablePath);
        var automation = new UIA3Automation();

        try
        {
            var mainWindow = application.GetMainWindow(
                automation,
                StartupTimeout);

            if (mainWindow is null)
            {
                throw new InvalidOperationException(
                    $"The WPF main window did not appear within {StartupTimeout}.");
            }

            return new WpfApplicationFixture(
                application,
                automation,
                mainWindow);
        }
        catch
        {
            automation.Dispose();

            if (!application.HasExited)
            {
                application.Kill();
            }

            application.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        Automation.Dispose();

        if (!Application.HasExited)
        {
            Application.Close(killIfCloseFails: true);
        }

        Application.Dispose();
    }

    private static string ResolveExecutablePath()
    {
        var configuredPath =
            Environment.GetEnvironmentVariable(ExecutableEnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullConfiguredPath = Path.GetFullPath(configuredPath);

            if (!File.Exists(fullConfiguredPath))
            {
                throw new FileNotFoundException(
                    $"{ExecutableEnvironmentVariable} points to a missing file.",
                    fullConfiguredPath);
            }

            return fullConfiguredPath;
        }

        var solutionRoot = FindSolutionRoot(AppContext.BaseDirectory);
        var configuration = AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
        var executablePath = Path.Combine(
            solutionRoot,
            "src",
            "Clients",
            "WpfClient",
            "bin",
            "x64",
            configuration,
            "net9.0-windows",
            "WpfClient.exe");

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                "The WPF client executable was not found. Build WpfClient first "
                + $"or set {ExecutableEnvironmentVariable}.",
                executablePath);
        }

        return executablePath;
    }

    private static string FindSolutionRoot(string startingDirectory)
    {
        for (var directory = new DirectoryInfo(startingDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClientAgnostic.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate the solution root from the test output directory.");
    }
}
