using System;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Web;
using DevExpress.Web;

namespace TestApplication.Web {
    public class Global : System.Web.HttpApplication {
        public Global() {
            InitializeComponent();
        }

        protected void Application_Start(Object sender, EventArgs e) {
//            RouteTable.Routes.RegisterXafRoutes();
            ASPxWebControl.CallbackError += Application_Error;
            DevExpress.ExpressApp.Web.TestScripts.TestScriptsManager.EasyTestEnabled = true;

        }
        protected void Session_Start(Object sender, EventArgs e) {
            Tracing.Initialize();

            var application = new TestWebApplication();
            application.Modules.Add(new WebModule());
            WebApplication.SetInstance(Session, application);
//            SecurityStrategy security = (SecurityStrategy)WebApplication.Instance.Security;
//            security.RegisterXPOAdapterProviders();
            DevExpress.ExpressApp.Web.Templates.DefaultVerticalTemplateContentNew.ClearSizeLimit();
            WebApplication.Instance.SwitchToNewStyle();
            


            WebApplication.Instance.ConnectionString = "XpoProvider=InMemoryDataSet";

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
