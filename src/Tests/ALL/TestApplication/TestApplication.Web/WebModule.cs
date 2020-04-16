using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.AuditTrail;
using DevExpress.ExpressApp.Chart;
using DevExpress.ExpressApp.Chart.Web;
using DevExpress.ExpressApp.CloneObject;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Dashboards;
using DevExpress.ExpressApp.Dashboards.Web;
using DevExpress.ExpressApp.FileAttachments.Web;
using DevExpress.ExpressApp.HtmlPropertyEditor.Web;
using DevExpress.ExpressApp.Kpi;
using DevExpress.ExpressApp.Maps.Web;
using DevExpress.ExpressApp.MiddleTier;
using DevExpress.ExpressApp.Notifications;
using DevExpress.ExpressApp.Notifications.Web;
using DevExpress.ExpressApp.Objects;
using DevExpress.ExpressApp.PivotChart;
using DevExpress.ExpressApp.PivotChart.Web;
using DevExpress.ExpressApp.PivotGrid;
using DevExpress.ExpressApp.PivotGrid.Web;
using DevExpress.ExpressApp.ReportsV2;
using DevExpress.ExpressApp.ReportsV2.Web;
using DevExpress.ExpressApp.Scheduler;
using DevExpress.ExpressApp.ScriptRecorder;
using DevExpress.ExpressApp.ScriptRecorder.Web;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.Xpo;
using DevExpress.ExpressApp.StateMachine;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.TreeListEditors;
using DevExpress.ExpressApp.TreeListEditors.Web;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Validation;
using DevExpress.ExpressApp.Validation.Web;
using DevExpress.ExpressApp.ViewVariantsModule;
using DevExpress.ExpressApp.Web.SystemModule;
using DevExpress.ExpressApp.Workflow;
using TestApplication.Web.LookupCascade;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib.BO;
using Xpand.XAF.Modules.AutoCommit;
using Xpand.XAF.Modules.CloneMemberValue;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.HideToolBar;
using Xpand.XAF.Modules.LookupCascade;
using Xpand.XAF.Modules.MasterDetail;
using Xpand.XAF.Modules.ModelMapper;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelViewInheritance;
using Xpand.XAF.Modules.ProgressBarViewItem;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.RefreshView;
using Xpand.XAF.Modules.SuppressConfirmation;
using Xpand.XAF.Modules.ViewEditMode;

namespace TestApplication.Web{
    public class WebModule:ModuleBase{
        public WebModule(){
            #region XAF Modules

            RequiredModuleTypes.Add(typeof(ScriptRecorderModuleBase));
            RequiredModuleTypes.Add(typeof(ScriptRecorderAspNetModule));
            RequiredModuleTypes.Add(typeof(AuditTrailModule));
            RequiredModuleTypes.Add(typeof(ChartModule));
            RequiredModuleTypes.Add(typeof(ChartAspNetModule));
            RequiredModuleTypes.Add(typeof(AuditTrailModule));
            RequiredModuleTypes.Add(typeof(ChartModule));
            RequiredModuleTypes.Add(typeof(ChartAspNetModule));
            RequiredModuleTypes.Add(typeof(CloneObjectModule));
            RequiredModuleTypes.Add(typeof(ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(DashboardsModule));
            RequiredModuleTypes.Add(typeof(DashboardsAspNetModule));
            RequiredModuleTypes.Add(typeof(FileAttachmentsAspNetModule));
            RequiredModuleTypes.Add(typeof(HtmlPropertyEditorAspNetModule));
            RequiredModuleTypes.Add(typeof(KpiModule));
            RequiredModuleTypes.Add(typeof(MapsAspNetModule));
            RequiredModuleTypes.Add(typeof(NotificationsModule));
            RequiredModuleTypes.Add(typeof(NotificationsAspNetModule));
            RequiredModuleTypes.Add(typeof(BusinessClassLibraryCustomizationModule));
            RequiredModuleTypes.Add(typeof(PivotChartModuleBase));
            RequiredModuleTypes.Add(typeof(PivotChartAspNetModule));
            RequiredModuleTypes.Add(typeof(PivotGridModule));
            RequiredModuleTypes.Add(typeof(PivotGridAspNetModule));
            RequiredModuleTypes.Add(typeof(ReportsModuleV2));
            RequiredModuleTypes.Add(typeof(ReportsAspNetModuleV2));
            RequiredModuleTypes.Add(typeof(SchedulerModuleBase));
            RequiredModuleTypes.Add(typeof(ScriptRecorderModuleBase));
            RequiredModuleTypes.Add(typeof(ScriptRecorderAspNetModule));
            RequiredModuleTypes.Add(typeof(SecurityModule));
            RequiredModuleTypes.Add(typeof(SecurityXpoModule));
            RequiredModuleTypes.Add(typeof(StateMachineModule));
            RequiredModuleTypes.Add(typeof(TreeListEditorsModuleBase));
            RequiredModuleTypes.Add(typeof(TreeListEditorsAspNetModule));
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ValidationModule));
            RequiredModuleTypes.Add(typeof(ValidationAspNetModule));
            RequiredModuleTypes.Add(typeof(ViewVariantsModule));
            RequiredModuleTypes.Add(typeof(SystemAspNetModule));
            RequiredModuleTypes.Add(typeof(WorkflowModule));
            RequiredModuleTypes.Add(typeof(ServerUpdateDatabaseModule));

            #endregion
            AdditionalExportedTypes.Add(typeof(Order));
            AdditionalExportedTypes.Add(typeof(Customer));
//
            RequiredModuleTypes.Add(typeof(AutoCommitModule));
            RequiredModuleTypes.Add(typeof(CloneMemberValueModule));
            RequiredModuleTypes.Add(typeof(CloneModelViewModule));
            RequiredModuleTypes.Add(typeof(HideToolBarModule));
            RequiredModuleTypes.Add(typeof(MasterDetailModule));
            RequiredModuleTypes.Add(typeof(ModelMapperModule));
            RequiredModuleTypes.Add(typeof(ModelViewInheritanceModule));
            RequiredModuleTypes.Add(typeof(ProgressBarViewItemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
//            RequiredModuleTypes.Add(typeof(ReactiveLoggerHubModule));
            RequiredModuleTypes.Add(typeof(RefreshViewModule));
            RequiredModuleTypes.Add(typeof(SuppressConfirmationModule));
            RequiredModuleTypes.Add(typeof(ViewEditModeModule));
            RequiredModuleTypes.Add(typeof(LookupCascadeModule));
            
        }

        // protected override IEnumerable<Type> GetDeclaredControllerTypes(){
        //     yield return typeof(CustomerController);
        // }
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB){
            yield return new OrderModuleUpdater(objectSpace, versionFromDB);
        }

        public override void Setup(XafApplication application){
            base.Setup(application);
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            
            moduleManager.Extend(Enum.GetValues(typeof(PredefinedMap)).OfType<PredefinedMap>().Where(map =>map!=PredefinedMap.None&& map.Platform()==Platform.Web));
            moduleManager.LookupCascade()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

    }

    public class CustomerController : ObjectViewController<ListView,Customer>{
        protected override void OnViewControlsCreated(){
            base.OnViewControlsCreated();
            // throw new NotImplementedException();
        }
    }
}