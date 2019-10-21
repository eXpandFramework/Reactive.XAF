using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Win;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using DevExpress.XtraEditors;
using TestApplication.Win;
using Xpand.Extensions.Linq;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Reactive.Services;
using FileLocation = DevExpress.Persistent.Base.FileLocation;

namespace ALL.Win.Tests {
    [DefaultClassOptions]
    public class Customer:Person{
        public Customer(Session session) : base(session){
        }
    }
    public class WinModule:ModuleBase{
        public WinModule(){
            AdditionalExportedTypes.Add(typeof(Customer));
        }
    }

    public class TestWinApplication:TestApplicationWindowsFormsApplication{
        
    }
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {

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
            winApplication.Modules.Add(new WinModule());
            
            winApplication.RegisterInMemoryObjectSpaceProvider();
            winApplication.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
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
