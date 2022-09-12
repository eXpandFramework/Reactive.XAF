using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;
using Xpand.XAF.ModelEditor.Module.Win;

// using Xpand.XAF.ModelEditor.Module.Win;

namespace Xpand.XAF.ModelEditor.Win {
    public partial class ModelEditorWindowsFormsApplication : WinApplication {
        public ModelEditorWindowsFormsApplication() => InitializeComponent();

        public override void StartSplash() { }

        protected override void OnCustomGetUserModelDifferencesPath(CustomGetUserModelDifferencesPathEventArgs args) {
            base.OnCustomGetUserModelDifferencesPath(args);
            args.Path = MEService.MeInstallationPath;
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            args.ObjectSpaceProviders.Add(new XPObjectSpaceProvider(XPObjectSpaceProvider.GetDataStoreProvider(InMemoryDataStoreProvider.ConnectionString, args.Connection, true), false));
            args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
        }

        private void ModelEditorWindowsFormsApplication_CustomizeLanguagesList(object sender, CustomizeLanguagesListEventArgs e) {
            string userLanguageName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            if(userLanguageName != "en-US" && e.Languages.IndexOf(userLanguageName) == -1) {
                e.Languages.Add(userLanguageName);
            }
        }



        private void ModelEditorWindowsFormsApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e) {
            e.Updater.Update();
            e.Handled = true;
        }
    }
}
