using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Utils;

namespace Xpand.XAF.SpeechManager.Win {
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Win.WinApplication._members
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public class SpeechManagerWindowsFormsApplication : WinApplication {
        public SpeechManagerWindowsFormsApplication() {
            SplashScreen = new DXSplashScreen(typeof(XafSplashScreen), new DefaultOverlayFormOptions());
            ApplicationName = "Xpand.XAF.SpeechManager";
            CheckCompatibilityType = CheckCompatibilityType.DatabaseSchema;
            UseOldTemplates = false;
            DatabaseVersionMismatch += SpeechManagerWindowsFormsApplication_DatabaseVersionMismatch;
            CustomizeLanguagesList += SpeechManagerWindowsFormsApplication_CustomizeLanguagesList;
        }
        private void SpeechManagerWindowsFormsApplication_CustomizeLanguagesList(object sender, CustomizeLanguagesListEventArgs e) {
            string userLanguageName = Thread.CurrentThread.CurrentUICulture.Name;
            if(userLanguageName != "en-US" && e.Languages.IndexOf(userLanguageName) == -1) {
                e.Languages.Add(userLanguageName);
            }
        }
        private void SpeechManagerWindowsFormsApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e) {
            e.Updater.Update();
            e.Handled = true;
        }
    }
}
