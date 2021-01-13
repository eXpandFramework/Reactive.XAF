using System;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.Persistent.Base;
using DevExpress.XtraEditors;
using Fasterflect;
using HarmonyLib;
using TestApplication.Module.Common;
using TestApplication.Module.ModelViewInheritance;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using FileLocation = DevExpress.Persistent.Base.FileLocation;

namespace TestApplication.Win {
    
    static class Program {
        public static void CollectModelStores(ref ModelStoreBase[] __result) {
            // MessageBox.Show("");
        }


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(){
            AppDomain.CurrentDomain.Patch(harmony => {
                harmony.Patch(typeof(ApplicationModelManager).Method("CollectModelStores",Flags.StaticAnyVisibility),postfix:new HarmonyMethod(typeof(Program),nameof(CollectModelStores)));
            });
            DevExpress.ExpressApp.Win.EasyTest.EasyTestRemotingRegistration.Register();
            
            WindowsFormsSettings.LoadApplicationSettings();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#if !NETCOREAPP3_1
            DevExpress.ExpressApp.Security.EditModelPermission.AlwaysGranted = System.Diagnostics.Debugger.IsAttached;
#endif
            if(Tracing.GetFileLocationFromSettings() == FileLocation.CurrentUserApplicationDataFolder) {
                Tracing.LocalUserAppDataPath = Application.LocalUserAppDataPath;
            }
            Tracing.Initialize();
            var winApplication = new TestWinApplication();

            winApplication.ConfigureConnectionString();

            try{
                
                winApplication.Setup();
                if (!AppDomain.CurrentDomain.UseNetFramework()){
                    ((IModelApplicationOptionsSkin) winApplication.Model.Options).Skin = "The Bezier";    
                }
                winApplication.Start();
            }
            catch(Exception e) {
                winApplication.HandleException(e);
            }
        }
    }
}
