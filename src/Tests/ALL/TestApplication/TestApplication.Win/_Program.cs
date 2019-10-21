using System;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Forms;

using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Win;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;

namespace TestApplication.Win {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(params string[] args) {
#if EASYTEST
            DevExpress.ExpressApp.Win.EasyTest.EasyTestRemotingRegistration.Register();
            TestApplication.EasyTest.InMemoryDataStoreProvider.Register();
#endif

            Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			EditModelPermission.AlwaysGranted = System.Diagnostics.Debugger.IsAttached;
			TestApplicationWindowsFormsApplication winApplication = new TestApplicationWindowsFormsApplication();
#if EASYTEST
			/*if(ConfigurationManager.ConnectionStrings["EasyTestConnectionString"] != null) {
				winApplication.ConnectionString = ConfigurationManager.ConnectionStrings["EasyTestConnectionString"].ConnectionString;
			}*/
            winApplication.ConnectionString = "XpoProvider=InMemoryDataSet";
#endif
			if(ConfigurationManager.ConnectionStrings["ConnectionString"] != null) {
				winApplication.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
			}
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