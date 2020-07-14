using System;
using System.Windows.Forms;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.XtraEditors;
using FileLocation = DevExpress.Persistent.Base.FileLocation;

namespace TestApplication.Win {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(){
            
            DevExpress.ExpressApp.Win.EasyTest.EasyTestRemotingRegistration.Register();

            WindowsFormsSettings.LoadApplicationSettings();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EditModelPermission.AlwaysGranted = System.Diagnostics.Debugger.IsAttached;
            if(Tracing.GetFileLocationFromSettings() == FileLocation.CurrentUserApplicationDataFolder) {
                Tracing.LocalUserAppDataPath = Application.LocalUserAppDataPath;
            }
            Tracing.Initialize();
            var winApplication = new TestWinApplication();

            
            try {
                winApplication.Setup();
                winApplication.Start();
            }
            catch(Exception e) {
                winApplication.HandleException(e);
            }
        }
    }
}
