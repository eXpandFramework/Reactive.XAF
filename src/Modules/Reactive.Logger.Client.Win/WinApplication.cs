using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;
using Xpand.XAF.Modules.GridListEditor;
using Xpand.XAF.Modules.OneView;
using Xpand.XAF.Modules.Reactive.Logger.Hub;

namespace Xpand.XAF.Modules.Reactive.Logger.Client.Win {
    public partial class ReactiveLoggerClientWinApplication : WinApplication,ILoggerHubClientApplication {
        

        #region Default XAF configuration options (https://www.devexpress.com/kb=T501418)
        static ReactiveLoggerClientWinApplication() {
            DevExpress.Persistent.Base.PasswordCryptographer.EnableRfc2898 = true;
            DevExpress.Persistent.Base.PasswordCryptographer.SupportLegacySha512 = false;
			DevExpress.ExpressApp.Utils.ImageLoader.Instance.UseSvgImages = true;
        }
        private void InitializeDefaults(){
            Title = "RXLoggerClient";
            LinkNewObjectToParentImmediately = false;
            OptimizedControllersCreation = true;
            UseLightStyle = true;
//			SplashScreen = new DXSplashScreen(typeof(XafSplashScreen), new DefaultOverlayFormOptions());
			ExecuteStartupLogicBeforeClosingLogonWindow = true;
        }
        #endregion
        public ReactiveLoggerClientWinApplication() {
            InitializeComponent();
            InitializeDefaults();
            Modules.AddRange(new ModuleBase[]{
                new ReactiveLoggerHubModule(),
                new OneViewModule(),
                new GridListEditorModule() 
            });
        }
        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            args.ObjectSpaceProviders.Add(new XPObjectSpaceProvider(
                XPObjectSpaceProvider.GetDataStoreProvider(args.ConnectionString, args.Connection, true), true));
            args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
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
