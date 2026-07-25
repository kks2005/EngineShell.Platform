namespace Wpf.UiTests.Infrastructure;

public abstract class UiTestBase
    : UiTest.Infrastructure.UiTestBase<WpfApplicationFixture>
{
    protected override TimeSpan ObservationDelay => TimeSpan.FromSeconds(2);

    protected override WpfApplicationFixture LaunchApplication() =>
        WpfApplicationFixture.Launch();
}
