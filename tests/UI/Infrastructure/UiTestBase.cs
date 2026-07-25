namespace Wpf.UiTests.Infrastructure;

public abstract class UiTestBase
{
    private static readonly TimeSpan ObservationDelay = TimeSpan.FromSeconds(2);
    private WpfApplicationFixture? _fixture;

    public TestContext TestContext { get; set; } = null!;

    protected WpfApplicationFixture App =>
        _fixture
        ?? throw new InvalidOperationException(
            "The WPF application fixture has not been initialized.");

    [TestInitialize]
    public void StartApplication()
    {
        _fixture = WpfApplicationFixture.Launch();
    }

    [TestCleanup]
    public async Task StopApplication()
    {
        if (_fixture is null)
        {
            return;
        }

        try
        {
            await Task.Delay(ObservationDelay);

            if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
            {
                var screenshotPath = ScreenshotCapture.CaptureFailure(
                    _fixture.MainWindow,
                    TestContext.TestName ?? "UnknownTest",
                    TestContext.ResultsDirectory);
                TestContext.AddResultFile(screenshotPath);
            }
        }
        finally
        {
            _fixture.Dispose();
            _fixture = null;
        }
    }
}
