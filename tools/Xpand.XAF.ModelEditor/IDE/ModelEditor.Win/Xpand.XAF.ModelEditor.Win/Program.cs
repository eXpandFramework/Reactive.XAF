using System;
using System.Configuration;
using System.Net.Http;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.XtraEditors;
using Xpand.Extensions.JsonExtensions;
using Xpand.XAF.ModelEditor.Module.Win;

namespace Xpand.XAF.ModelEditor.Win {
    static class Program {
        [STAThread]
        static void Main(){
            
           
           
            FrameworkSettings.DefaultSettingsCompatibilityMode = FrameworkSettingsCompatibilityMode.Latest;
#if EASYTEST
            DevExpress.ExpressApp.Win.EasyTest.EasyTestRemotingRegistration.Register();
#endif
            WindowsFormsSettings.LoadApplicationSettings();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            DevExpress.Utils.ToolTipController.DefaultController.ToolTipType = DevExpress.Utils.ToolTipType.SuperTip;
            if (Tracing.GetFileLocationFromSettings() == FileLocation.CurrentUserApplicationDataFolder) {
                Tracing.LocalUserAppDataPath = Application.LocalUserAppDataPath;
            }

            Tracing.Initialize();
            ModelEditorWindowsFormsApplication winApplication = new ModelEditorWindowsFormsApplication();
            if (ConfigurationManager.ConnectionStrings["ConnectionString"] != null) {
                winApplication.ConnectionString =
                    ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            }
#if EASYTEST
            if(ConfigurationManager.ConnectionStrings["EasyTestConnectionString"] != null) {
                winApplication.ConnectionString =
 ConfigurationManager.ConnectionStrings["EasyTestConnectionString"].ConnectionString;
            }
#endif
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached &&
                winApplication.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
                winApplication.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
            }
#endif
            try {
                winApplication.Setup();
                winApplication.Start();
            }
            catch (Exception e) {
                winApplication.StopSplash();
                winApplication.HandleException(e);
            }
        }
    }
}