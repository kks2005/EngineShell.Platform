namespace WinUI.UiTests.Infrastructure;

public abstract class UiTestBase
    : UiTest.Infrastructure.UiTestBase<WinUIApplicationFixture>
{
    protected override WinUIApplicationFixture LaunchApplication() =>
        WinUIApplicationFixture.Launch();
}
