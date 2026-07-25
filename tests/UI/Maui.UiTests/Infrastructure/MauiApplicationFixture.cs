using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using UiTest.Infrastructure;

namespace Maui.UiTests.Infrastructure;

public sealed class MauiApplicationFixture : IUiApplicationFixture
{
    private const string ExecutableEnvironmentVariable = "MAUI_CLIENT_EXE";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(25);

    private MauiApplicationFixture(
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

    public static MauiApplicationFixture Launch()
    {
        var application = Application.Launch(ResolveExecutablePath());
        var automation = new UIA3Automation();

        try
        {
            var mainWindow = application.GetMainWindow(automation, StartupTimeout)
                ?? throw new InvalidOperationException(
                    $"The MAUI main window did not appear within {StartupTimeout}.");

            return new MauiApplicationFixture(application, automation, mainWindow);
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
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";

        var baseOutput = Path.Combine(
            root,
            "src",
            "Clients",
            "MauiClient",
            "bin",
            configuration,
            "net9.0-windows10.0.19041.0");

        var candidates = new[]
        {
            Path.Combine(baseOutput, "win10-x64", "MauiClient.exe"),
            Path.Combine(baseOutput, "win-x64", "MauiClient.exe"),
            Path.Combine(baseOutput, "MauiClient.exe")
        };

        var executablePath = candidates.FirstOrDefault(File.Exists);
        if (executablePath is not null)
        {
            return executablePath;
        }

        throw new FileNotFoundException(
            "Build MauiClient (Windows target) first or set MAUI_CLIENT_EXE.",
            string.Join(Environment.NewLine, candidates));
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

        throw new DirectoryNotFoundException("Could not locate the solution root.");
    }
}