using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;

namespace TestApplication.Win
{
    public partial class TestApplicationWindowsFormsApplication : WinApplication
    {
        public TestApplicationWindowsFormsApplication()
        {
            InitializeComponent();
            DelayedViewItemsInitialization = true;
#if EASYTEST
            DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
            CheckCompatibilityType = CheckCompatibilityType.ModuleInfo;
#endif
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args)
        {
            args.ObjectSpaceProvider = new XPObjectSpaceProvider(new ConnectionStringDataStoreProvider(args.ConnectionString), false);
        }

        private void TestApplicationWindowsFormsApplication_DatabaseVersionMismatch(object sender, DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs e)
        {
#if EASYTEST
            e.Updater.Update();
            e.Handled = true;
            TestApplication.EasyTest.InMemoryDataStoreProvider.Save();
#else
			if(System.Diagnostics.Debugger.IsAttached) {
				e.Updater.Update();
				e.Handled = true;
			}
			else {
				throw new InvalidOperationException(
					"The application cannot connect to the specified database, because the latter doesn't exist or its version is older than that of the application.\r\n" +
					"This error occurred  because the automatic database update was disabled when the application was started without debugging.\r\n" +
					"To avoid this error, you should either start the application under Visual Studio in debug mode, or modify the " +
					"source code of the 'DatabaseVersionMismatch' event handler to enable automatic database update, " +
					"or manually create a database using the 'DBUpdater' tool.\r\n" +
					"Anyway, refer to the 'Update Application and Database Versions' help topic at http://www.devexpress.com/Help/?document=ExpressApp/CustomDocument2795.htm " +
					"for more detailed information. If this doesn't help, please contact our Support Team at http://www.devexpress.com/Support/Center/");
			}
#endif
        }
    }
}
