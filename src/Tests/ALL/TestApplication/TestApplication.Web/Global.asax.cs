using System;
using System.Configuration;
using System.Web.Configuration;
using System.Web;
using System.Web.Routing;

using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Web;
using DevExpress.Web;
using TestApplication.Web;

namespace TestApplication.Web {
    public class Global : System.Web.HttpApplication {
        public Global() {
            InitializeComponent();
        }
#if EASYTEST
        protected void Application_AcquireRequestState(Object sender, EventArgs e)
        {
            if (HttpContext.Current.Request.Params["Reset"] == "true")
            {
                TestApplication.EasyTest.InMemoryDataStoreProvider.Reload();
                WebApplication.Instance.LogOff();
                WebApplication.Redirect(Request.RawUrl.Replace("&Reset=true", "").Replace("?Reset=true", ""), true);
            }
        }
#endif
        protected void Application_Start(Object sender, EventArgs e) {
//            RouteTable.Routes.RegisterXafRoutes();
            ASPxWebControl.CallbackError += new EventHandler(Application_Error);
#if EASYTEST
            DevExpress.ExpressApp.Web.TestScripts.TestScriptsManager.EasyTestEnabled = true;
            ConfirmationsHelper.IsConfirmationsEnabled = false;
            TestApplication.EasyTest.InMemoryDataStoreProvider.Register();
#endif
        }
        protected void Session_Start(Object sender, EventArgs e) {
            Tracing.Initialize();
#if EASYTEST
            TestApplication.EasyTest.InMemoryDataStoreProvider.Reload();
#endif
            var application = new TestApplicationAspNetApplication();
            WebApplication.SetInstance(Session, application);
            SecurityStrategy security = (SecurityStrategy)WebApplication.Instance.Security;
//            security.RegisterXPOAdapterProviders();
            DevExpress.ExpressApp.Web.Templates.DefaultVerticalTemplateContentNew.ClearSizeLimit();
            WebApplication.Instance.SwitchToNewStyle();
            if(ConfigurationManager.ConnectionStrings["ConnectionString"] != null) {
                WebApplication.Instance.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            }
#if EASYTEST
            TestApplication.EasyTest.InMemoryDataStoreProvider.Reload();
            if (ConfigurationManager.ConnectionStrings["EasyTestConnectionString"] != null) {
                WebApplication.Instance.ConnectionString = ConfigurationManager.ConnectionStrings["EasyTestConnectionString"].ConnectionString;
            }
#endif

#if EASYTEST
            WebApplication.Instance.ConnectionString = "XpoProvider=InMemoryDataSet";
#endif
#if DEBUG
            if(System.Diagnostics.Debugger.IsAttached && WebApplication.Instance.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
                WebApplication.Instance.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
            }
#endif
            WebApplication.Instance.Setup();
            WebApplication.Instance.Start();
        }
        protected void Application_BeginRequest(Object sender, EventArgs e) {
        }
        protected void Application_EndRequest(Object sender, EventArgs e) {
        }
        protected void Application_AuthenticateRequest(Object sender, EventArgs e) {
        }
        protected void Application_Error(Object sender, EventArgs e) {
            ErrorHandling.Instance.ProcessApplicationError();
        }
        protected void Session_End(Object sender, EventArgs e) {
            WebApplication.LogOff(Session);
            WebApplication.DisposeInstance(Session);
        }
        protected void Application_End(Object sender, EventArgs e) {
        }
#region Web Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
        }
#endregion
    }
}
