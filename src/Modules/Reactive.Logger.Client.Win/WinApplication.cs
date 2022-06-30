using System;
using System.Windows;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using Xpand.XAF.Modules.GridListEditor;
using Xpand.XAF.Modules.OneView;
using Xpand.XAF.Modules.Reactive.Logger.Hub;
using Xpand.XAF.Modules.Windows;
using Application = System.Windows.Forms.Application;

namespace Xpand.XAF.Modules.Reactive.Logger.Client.Win {
    public partial class ReactiveLoggerClientWinApplication : WinApplication,ILoggerHubClientApplication {
        

        #region Default XAF configuration options (https://www.devexpress.com/kb=T501418)
        static ReactiveLoggerClientWinApplication() {
            PasswordCryptographer.EnableRfc2898 = true;
            PasswordCryptographer.SupportLegacySha512 = false;
			DevExpress.ExpressApp.Utils.ImageLoader.Instance.UseSvgImages = true;
        }
        private void InitializeDefaults(){
            Title = "RXLoggerClient";
            LinkNewObjectToParentImmediately = false;
            OptimizedControllersCreation = true;
            UseLightStyle = true;
            ExecuteStartupLogicBeforeClosingLogonWindow = true;
        }
        #endregion
        public ReactiveLoggerClientWinApplication() {
            InitializeComponent();
            InitializeDefaults();
            
            Modules.AddRange(new ModuleBase[]{ new ReactiveLoggerHubModule(), new OneViewModule(),
                new GridListEditorModule(),new WindowsModule(),new ReactiveLoggerClientModule() 
            });
        }
        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            args.ObjectSpaceProviders.Add(new XPObjectSpaceProvider(
                XPObjectSpaceProvider.GetDataStoreProvider(args.ConnectionString, args.Connection, true), true));
            args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
        }

        protected override void OnCustomGetUserModelDifferencesPath(CustomGetUserModelDifferencesPathEventArgs args) {
            base.OnCustomGetUserModelDifferencesPath(args);
            args.Path = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Xpand.Reactive.Logger.Client";
        }

        private void LanguagesList(object sender, CustomizeLanguagesListEventArgs e) {
            string userLanguageName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            if(userLanguageName != "en-US" && e.Languages.IndexOf(userLanguageName) == -1) {
                e.Languages.Add(userLanguageName);
            }
        }
        private void WindowsFormsApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e) {
            e.Updater.Update();
            e.Handled = true;
        }
    }
}
