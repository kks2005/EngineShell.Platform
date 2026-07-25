namespace Wpf.UiTests.PageObjects;

internal static class AutomationIds
{
    internal static class Shell
    {
        public const string MainWindow = "Shell.MainWindow";
    }

    internal static class Header
    {
        public const string Navigation = "Header.Navigation";
        public const string GeneralItem = "Header.GeneralItem";
        public const string ScreensItem = "Header.ScreensItem";
        public const string RenderEngineItem = "Header.RenderEngineItem";
    }

    internal static class General
    {
        public const string Title = "General.TitleText";
        public const string UserNameTextBox = "General.UserNameTextBox";
        public const string ThemeComboBox = "General.ThemeComboBox";
        public const string SaveButton = "General.SaveButton";
    }

    internal static class Screens
    {
        public const string LoadButton = "Screens.LoadButton";
        public const string RefreshButton = "Screens.RefreshButton";
        public const string BusyText = "Screens.BusyText";
        public const string List = "Screens.List";
    }

    internal static class RenderEngine
    {
        public const string Title = "RenderEngine.TitleText";
        public const string LoadButton = "RenderEngine.LoadButton";
        public const string ProcessButton = "RenderEngine.ProcessButton";
        public const string CancelButton = "RenderEngine.CancelButton";
        public const string StatusText = "RenderEngine.StatusText";
        public const string ProgressBar = "RenderEngine.ProgressBar";
        public const string ProgressText = "RenderEngine.ProgressText";
        public const string RawEngineEventCountText =
            "RenderEngine.RawEngineEventCountText";
        public const string DisplayedEngineEventCountText =
            "RenderEngine.DisplayedEngineEventCountText";
        public const string LastDisplayedEngineEventText =
            "RenderEngine.LastDisplayedEngineEventText";
    }

    internal static class Footer
    {
        public const string StatusText = "Footer.StatusText";
        public const string ProgressBar = "Footer.ProgressBar";
    }
}
