using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Chart.Web;
using DevExpress.ExpressApp.Dashboards.Web;
using DevExpress.ExpressApp.FileAttachments.Web;
using DevExpress.ExpressApp.HtmlPropertyEditor.Web;
using DevExpress.ExpressApp.Maps.Web;
using DevExpress.ExpressApp.Notifications.Web;
using DevExpress.ExpressApp.PivotChart.Web;
using DevExpress.ExpressApp.PivotGrid.Web;
using DevExpress.ExpressApp.ReportsV2.Web;
using DevExpress.ExpressApp.ScriptRecorder.Web;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.TreeListEditors.Web;
using DevExpress.ExpressApp.Validation.Web;
using DevExpress.ExpressApp.Web.SystemModule;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using TestApplication.Module.Web.LookupCascade;
using TestApplication.Module.Win.Office.Microsoft;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.LookupCascade;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace TestApplication.Module.Web {
    public class TestApplicationWebModule:ModuleBase,IWebModule{
        public TestApplicationWebModule() {
            RequiredModuleTypes.Add(typeof(TestApplicationModule));
            #region XAF Module
            RequiredModuleTypes.Add(typeof(ScriptRecorderAspNetModule));
            RequiredModuleTypes.Add(typeof(ChartAspNetModule));
            RequiredModuleTypes.Add(typeof(ChartAspNetModule));
            RequiredModuleTypes.Add(typeof(DashboardsAspNetModule));
            RequiredModuleTypes.Add(typeof(FileAttachmentsAspNetModule));
            RequiredModuleTypes.Add(typeof(HtmlPropertyEditorAspNetModule));
            RequiredModuleTypes.Add(typeof(MapsAspNetModule));
            RequiredModuleTypes.Add(typeof(NotificationsAspNetModule));
            RequiredModuleTypes.Add(typeof(PivotChartAspNetModule));
            RequiredModuleTypes.Add(typeof(PivotGridAspNetModule));
            RequiredModuleTypes.Add(typeof(ReportsAspNetModuleV2));
            RequiredModuleTypes.Add(typeof(ScriptRecorderAspNetModule));
            RequiredModuleTypes.Add(typeof(TreeListEditorsAspNetModule));
            RequiredModuleTypes.Add(typeof(ValidationAspNetModule));
            RequiredModuleTypes.Add(typeof(SystemAspNetModule));
            #endregion
            RequiredModuleTypes.Add(typeof(LookupCascadeModule));
            RequiredModuleTypes.Add(typeof(MicrosoftModule));
            RequiredModuleTypes.Add(typeof(MicrosoftTodoModule));
            RequiredModuleTypes.Add(typeof(MicrosoftCalendarModule));
        }

        public override void Setup(XafApplication application){
            base.Setup(application);
            application.Security = new SecurityStrategyComplex(typeof(PermissionPolicyUser),
                typeof(PermissionPolicyRole), new AuthenticationStandard(typeof(PermissionPolicyUser), typeof(AuthenticationStandardLogonParameters)));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            

            if (!Debugger.IsAttached){

                moduleManager.Extend(Enum.GetValues(typeof(PredefinedMap)).OfType<PredefinedMap>().Where(map =>map!=PredefinedMap.None&& map.Platform()==Platform.Web));

            }
            moduleManager.LookupCascade().ToUnit()
                .Merge(moduleManager.ConnectMicrosoftCalendarService())
                .Merge(moduleManager.ConnectMicrosoftService())
                .Merge(moduleManager.ConnectMicrosoftTodoService())
                .Subscribe(this);
        }

    }

    
}