using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;

namespace UiTest.Infrastructure;

public static class ScreenshotCapture
{
    public static string CaptureFailure(
        Window window,
        string testName,
        string? resultsDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(resultsDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "TestResults", "Screenshots")
            : Path.Combine(resultsDirectory, "Screenshots");
        Directory.CreateDirectory(directory);

        var safeTestName = string.Concat(
            testName.Select(character =>
                Path.GetInvalidFileNameChars().Contains(character)
                    ? '_'
                    : character));
        var filePath = Path.Combine(
            directory,
            $"{safeTestName}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png");

        using var image = Capture.Element(window);
        image.ToFile(filePath);
        return filePath;
    }
}
