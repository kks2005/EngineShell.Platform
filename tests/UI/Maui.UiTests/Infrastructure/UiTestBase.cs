namespace Maui.UiTests.Infrastructure;

public abstract class UiTestBase
    : UiTest.Infrastructure.UiTestBase<MauiApplicationFixture>
{
    protected override MauiApplicationFixture LaunchApplication() =>
        MauiApplicationFixture.Launch();
}