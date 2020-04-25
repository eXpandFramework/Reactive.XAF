using DevExpress.ExpressApp;
using DevExpress.ExpressApp.AuditTrail;
using DevExpress.ExpressApp.Chart;
using DevExpress.ExpressApp.Chart.Win;
using DevExpress.ExpressApp.CloneObject;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Dashboards;
using DevExpress.ExpressApp.Dashboards.Win;
using DevExpress.ExpressApp.FileAttachments.Win;
using DevExpress.ExpressApp.HtmlPropertyEditor.Win;
using DevExpress.ExpressApp.Kpi;
using DevExpress.ExpressApp.MiddleTier;
using DevExpress.ExpressApp.Notifications;
using DevExpress.ExpressApp.Notifications.Win;
using DevExpress.ExpressApp.Objects;
using DevExpress.ExpressApp.Office.Win;
using DevExpress.ExpressApp.PivotChart;
using DevExpress.ExpressApp.PivotChart.Win;
using DevExpress.ExpressApp.PivotGrid;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.ExpressApp.ReportsV2;
using DevExpress.ExpressApp.ReportsV2.Win;
using DevExpress.ExpressApp.Scheduler;
using DevExpress.ExpressApp.Scheduler.Win;
using DevExpress.ExpressApp.ScriptRecorder;
using DevExpress.ExpressApp.ScriptRecorder.Win;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.Xpo;
using DevExpress.ExpressApp.StateMachine;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.TreeListEditors;
using DevExpress.ExpressApp.TreeListEditors.Win;
using DevExpress.ExpressApp.Validation;
using DevExpress.ExpressApp.Validation.Win;
using DevExpress.ExpressApp.ViewVariantsModule;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.ExpressApp.Workflow;
using DevExpress.ExpressApp.Workflow.Win;
using Xpand.XAF.Modules.AutoCommit;
using Xpand.XAF.Modules.CloneMemberValue;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.GridListEditor;
using Xpand.XAF.Modules.HideToolBar;
using Xpand.XAF.Modules.MasterDetail;
using Xpand.XAF.Modules.ModelMapper;
using Xpand.XAF.Modules.ModelViewInheritance;
using Xpand.XAF.Modules.OneView;
using Xpand.XAF.Modules.ProgressBarViewItem;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Win;
using Xpand.XAF.Modules.RefreshView;
using Xpand.XAF.Modules.SequenceGenerator;
using Xpand.XAF.Modules.SuppressConfirmation;
using Xpand.XAF.Modules.ViewEditMode;

namespace TestApplication.Win{
    public class WinModule : ModuleBase{
        public WinModule(){
            #region XAF Modules

            RequiredModuleTypes.Add(typeof(AuditTrailModule));
            RequiredModuleTypes.Add(typeof(ChartModule));
            RequiredModuleTypes.Add(typeof(ChartWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(CloneObjectModule));
            RequiredModuleTypes.Add(typeof(ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(DashboardsModule));
            RequiredModuleTypes.Add(typeof(DashboardsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(FileAttachmentsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(HtmlPropertyEditorWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(KpiModule));
            RequiredModuleTypes.Add(typeof(NotificationsModule));
            RequiredModuleTypes.Add(typeof(NotificationsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(BusinessClassLibraryCustomizationModule));

            RequiredModuleTypes.Add(typeof(OfficeWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(PivotChartModuleBase));
            RequiredModuleTypes.Add(typeof(PivotChartWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(PivotGridModule));
            RequiredModuleTypes.Add(typeof(PivotGridWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ReportsModuleV2));
            RequiredModuleTypes.Add(typeof(ReportsWindowsFormsModuleV2));
            RequiredModuleTypes.Add(typeof(SchedulerModuleBase));
            RequiredModuleTypes.Add(typeof(SchedulerWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ScriptRecorderModuleBase));
            RequiredModuleTypes.Add(typeof(ScriptRecorderWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(SecurityModule));
            RequiredModuleTypes.Add(typeof(SecurityXpoModule));
            RequiredModuleTypes.Add(typeof(StateMachineModule));
            RequiredModuleTypes.Add(typeof(TreeListEditorsModuleBase));
            RequiredModuleTypes.Add(typeof(TreeListEditorsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ModuleBase));
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ValidationModule));
            RequiredModuleTypes.Add(typeof(ValidationWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ViewVariantsModule));
            RequiredModuleTypes.Add(typeof(SystemWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(WorkflowModule));
            RequiredModuleTypes.Add(typeof(WorkflowWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ServerUpdateDatabaseModule));

            #endregion
            
            AdditionalExportedTypes.Add(typeof(Customer));

            RequiredModuleTypes.Add(typeof(AutoCommitModule));
            RequiredModuleTypes.Add(typeof(CloneMemberValueModule));
            RequiredModuleTypes.Add(typeof(CloneModelViewModule));
            RequiredModuleTypes.Add(typeof(GridListEditorModule));
            RequiredModuleTypes.Add(typeof(HideToolBarModule));
            RequiredModuleTypes.Add(typeof(MasterDetailModule));
            RequiredModuleTypes.Add(typeof(ModelMapperModule));
            RequiredModuleTypes.Add(typeof(ModelViewInheritanceModule));
            RequiredModuleTypes.Add(typeof(OneViewModule));
            RequiredModuleTypes.Add(typeof(ProgressBarViewItemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
//            RequiredModuleTypes.Add(typeof(ReactiveLoggerHubModule));
            RequiredModuleTypes.Add(typeof(ReactiveModuleWin));
            RequiredModuleTypes.Add(typeof(RefreshViewModule));
            RequiredModuleTypes.Add(typeof(SuppressConfirmationModule));
            RequiredModuleTypes.Add(typeof(ViewEditModeModule));
            RequiredModuleTypes.Add(typeof(SequenceGeneratorModule));
            
            
        }
    }
}