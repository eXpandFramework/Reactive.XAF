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
using Xpand.Extensions.XAF.ModelExtensions;
using FileLocation = DevExpress.Persistent.Base.FileLocation;

namespace TestApplication.Win {
    
    static class Program {
        public static void GenerateModel(IModelViews views, IModelClass classInfo) {
            if (classInfo.TypeInfo.Type == typeof(ModelViewInheritance)) {
                var xml = @"
    <DetailView Id=""ModelViewInheritance_DetailView"">
      <Layout>
        <LayoutGroup Id=""Main"" RelativeSize=""100"">
          <LayoutGroup Id=""SimpleEditors"" RelativeSize=""34.632683658170912"">
            <LayoutGroup Id=""Person"" Index=""0"" RelativeSize=""53.246753246753244"">
              <LayoutGroup Id=""Person_col1"" RelativeSize=""50"">
                <LayoutItem Id=""LastName"" Index=""0"" RelativeSize=""36.363636363636367"" />
                <LayoutItem Id=""MiddleName"" Index=""1"" RelativeSize=""63.636363636363633"" />
                <LayoutItem Id=""FirstName"" Removed=""True"" />
              </LayoutGroup>
              <LayoutGroup Id=""Person_col2"" RelativeSize=""50"">
                <LayoutItem Id=""Birthday"" RelativeSize=""36.363636363636367"" />
                <LayoutItem Id=""FullName"" RelativeSize=""27.272727272727273"" />
                <LayoutItem Id=""Email"" RelativeSize=""36.363636363636367"" />
              </LayoutGroup>
            </LayoutGroup>
            <LayoutGroup Id=""Party"" Index=""1"" RelativeSize=""46.753246753246756"">
              <LayoutItem Id=""Photo"" RelativeSize=""27.777777777777779"" />
              <LayoutItem Id=""Address1"" RelativeSize=""22.222222222222221"" />
              <LayoutItem Id=""Address2"" RelativeSize=""22.222222222222221"" />
              <LayoutItem Id=""DisplayName"" RelativeSize=""27.777777777777779"" />
            </LayoutGroup>
          </LayoutGroup>
          <LayoutGroup Id=""PhoneNumbers_Group"" Direction=""Vertical"" RelativeSize=""65.367316341829081"">
            <LayoutItem Id=""PhoneNumbers"" RelativeSize=""100"" />
          </LayoutGroup>
        </LayoutGroup>
      </Layout>
    </DetailView>
";
                views["ModelViewInheritance_DetailView"].MergeWith(xml);
            }
        }

        public static void CreateUnchangeableLayer(ModelApplicationBase __result,ModelStoreBase[] modelDifferenceStores,
            bool cacheApplicationModelDifferences, ModelStoreBase applicationModelDifferenceStore) {
            // var modelApplicationBase = __result.GetLayer(38);
            // var modelApplication = __result.CreatorInstance.CreateModelApplication();          
            // var modelObjectView = __result.Application.Views[$"{nameof(ModelViewInheritanceBaseObject)}_DetailView"];
            // modelApplication.Application.ReadViewInLayer( modelObjectView, $"{nameof(ModelViewInheritance)}_DetailView");
            // modelApplicationBase.ReadFromModel(modelApplication);
            // // modelApplicationBase.MergeWith(modelApplication);
        } 

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(){
	        var harmony = new Harmony("typeof(IModelViewController).Namespace");
	        var harmonyPrefix = new HarmonyMethod(typeof(Program),nameof(GenerateModel));
	        // harmony.Patch(typeof(ModelDetailViewNodesGenerator).Method(nameof(ModelDetailViewNodesGenerator.GenerateModel),Flags.StaticAnyVisibility),postfix:harmonyPrefix);
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
