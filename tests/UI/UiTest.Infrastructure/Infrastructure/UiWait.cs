using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace UiTest.Infrastructure;

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

    public static AutomationElement ForElementByAutomationIdOrName(
        AutomationElement root,
        string value,
        TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () =>
                root.FindFirstDescendant(
                    condition => condition.ByAutomationId(value)) ??
                root.FindFirstDescendant(
                    condition => condition.ByName(value)),
            timeout ?? DefaultTimeout,
            DefaultInterval,
            throwOnTimeout: true,
            ignoreException: true,
            $"Element with automation ID or name '{value}' was not found.");

        return result.Result
            ?? throw new InvalidOperationException(
                $"Element with automation ID or name '{value}' was not found.");
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
