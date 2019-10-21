using System;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using DevExpress.XtraEditors;
using Xpand.XAF.Modules.Reactive.Services;
using FileLocation = DevExpress.Persistent.Base.FileLocation;

namespace TestApplication.Win {
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
        public TestWinApplication(){
            DatabaseUpdateMode=DatabaseUpdateMode.Never;
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args){
            args.ObjectSpaceProvider=new XPObjectSpaceProvider(new MemoryDataStoreProvider(),true);
        }
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
            
//            winApplication.RegisterInMemoryObjectSpaceProvider();
            winApplication.ConnectionString = InMemoryDataStoreProvider.ConnectionString;
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
