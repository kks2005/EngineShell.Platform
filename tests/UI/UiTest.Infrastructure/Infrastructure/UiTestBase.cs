using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UiTest.Infrastructure;

public abstract class UiTestBase<TFixture>
    where TFixture : class, IUiApplicationFixture
{
    private const string DesktopMutexName =
        @"Global\EngineShell.Platform.UiTests.Desktop";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(2);

    private Mutex? desktopMutex;
    private TFixture? fixture;

    public TestContext TestContext { get; set; } = null!;

    protected TFixture App =>
        fixture ?? throw new InvalidOperationException(
            "The UI application fixture has not been initialized.");

    protected virtual TimeSpan ObservationDelay => TimeSpan.Zero;

    protected abstract TFixture LaunchApplication();

    [TestInitialize]
    public void StartApplication()
    {
        desktopMutex = new Mutex(false, DesktopMutexName);

        try
        {
            if (!desktopMutex.WaitOne(LockTimeout))
            {
                throw new TimeoutException(
                    "Timed out waiting for exclusive access to the Windows desktop.");
            }
        }
        catch (AbandonedMutexException)
        {
            // Ownership is granted when the previous test process ended unexpectedly.
        }

        try
        {
            fixture = LaunchApplication();
        }
        catch
        {
            ReleaseDesktopLock();
            throw;
        }
    }

    [TestCleanup]
    public async Task StopApplication()
    {
        if (fixture is null)
        {
            ReleaseDesktopLock();
            return;
        }

        try
        {
            if (ObservationDelay > TimeSpan.Zero)
            {
                await Task.Delay(ObservationDelay);
            }

            if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
            {
                var screenshotPath = ScreenshotCapture.CaptureFailure(
                    fixture.MainWindow,
                    TestContext.TestName ?? "UnknownTest",
                    TestContext.ResultsDirectory);
                TestContext.AddResultFile(screenshotPath);
            }
        }
        finally
        {
            fixture.Dispose();
            fixture = null;
            ReleaseDesktopLock();
        }
    }

    private void ReleaseDesktopLock()
    {
        if (desktopMutex is null)
        {
            return;
        }

        try
        {
            desktopMutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // The mutex was created but ownership was never acquired.
        }
        finally
        {
            desktopMutex.Dispose();
            desktopMutex = null;
        }
    }
}
