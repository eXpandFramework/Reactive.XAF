using System;
using System.Windows.Forms;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.Persistent.Base;
using DevExpress.XtraEditors;
using TestApplication.Module.Common;
using Xpand.Extensions.AppDomainExtensions;
using FileLocation = DevExpress.Persistent.Base.FileLocation;

namespace TestApplication.Win {
    
    static class Program {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(){
	        // var harmony = new Harmony("typeof(IModelViewController).Namespace");
	        // var harmonyPrefix = new HarmonyMethod(typeof(ModelViewsNodesGeneratorPatch),nameof(ModelViewsNodesGeneratorPatch.GenerateModel));
	        // harmony.Patch(typeof(ModelDetailViewNodesGenerator).Method("GenerateModel",Flags.StaticPublic),postfix:harmonyPrefix);
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
