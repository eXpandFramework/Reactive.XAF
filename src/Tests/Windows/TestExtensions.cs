namespace Xpand.XAF.Modules.Windows.Tests {
    static class TestExtensions {

        public static void EnableExit(this IModelWindows modelWindows) {
            modelWindows.Exit.OnExit.HideMainWindow = true;
            modelWindows.Exit.Prompt.Enabled = true;
        }

    }
}