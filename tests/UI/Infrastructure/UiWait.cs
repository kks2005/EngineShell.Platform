using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace Wpf.UiTests.Infrastructure;

public static class UiWait
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan DefaultInterval =
        TimeSpan.FromMilliseconds(100);

    public static AutomationElement ForElement(
        AutomationElement root,
        string automationId,
        TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () => root.FindFirstDescendant(
                condition => condition.ByAutomationId(automationId)),
            timeout ?? DefaultTimeout,
            DefaultInterval,
            throwOnTimeout: true,
            ignoreException: true,
            $"Element '{automationId}' was not found.");

        return result.Result
            ?? throw new InvalidOperationException(
                $"Element '{automationId}' was not found.");
    }

    public static void Until(
        Func<bool> condition,
        string failureMessage,
        TimeSpan? timeout = null)
    {
        Retry.WhileFalse(
            condition,
            timeout ?? DefaultTimeout,
            DefaultInterval,
            throwOnTimeout: true,
            ignoreException: true,
            failureMessage);
    }
}
