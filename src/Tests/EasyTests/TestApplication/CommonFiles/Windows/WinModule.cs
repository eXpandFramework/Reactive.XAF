using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Chart.Win;
using DevExpress.ExpressApp.Dashboards.Win;
using DevExpress.ExpressApp.FileAttachments.Win;
using DevExpress.ExpressApp.Notifications.Win;
using DevExpress.ExpressApp.Office;
using DevExpress.ExpressApp.Office.Win;
using DevExpress.ExpressApp.PivotChart.Win;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.ExpressApp.ReportsV2.Win;
using DevExpress.ExpressApp.Scheduler.Win;
using DevExpress.ExpressApp.ScriptRecorder.Win;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.TreeListEditors.Win;
using DevExpress.ExpressApp.Validation.Win;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
#if !NETCOREAPP3_1_OR_GREATER
using System.Reactive.Linq;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo;
using Xpand.XAF.Modules.Office.DocumentStyleManager;
using TestApplication.Module.Win.Office.Microsoft;
using System.Collections.Generic;
using DevExpress.ExpressApp.Updating;
using TestApplication.Module.Win.Office.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Extensions;
#endif

namespace TestApplication.Module.Win {
    public sealed class TestApplicationWinModule : ModuleBase {
        public TestApplicationWinModule() {
            RequiredModuleTypes.Add(typeof(TestApplicationModule));
            AddXAFModules();
            AddXpandModules();
        }

        private void AddXpandModules() {
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.GridListEditor.GridListEditorModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.OneView.OneViewModule));
#if !NETCOREAPP3_1_OR_GREATER
            RequiredModuleTypes.Add(typeof(DocumentStyleManagerModule));
            RequiredModuleTypes.Add(typeof(MicrosoftModule));
            RequiredModuleTypes.Add(typeof(MicrosoftTodoModule));
            RequiredModuleTypes.Add(typeof(MicrosoftCalendarModule));
#endif
        }

        private void AddXAFModules() {
            RequiredModuleTypes.Add(typeof(ChartWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(DashboardsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(FileAttachmentsWindowsFormsModule));
#if !NETCOREAPP3_1_OR_GREATER
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.HtmlPropertyEditor.Win.HtmlPropertyEditorWindowsFormsModule));
#endif

            RequiredModuleTypes.Add(typeof(NotificationsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(OfficeWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(PivotChartWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(PivotGridWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ReportsWindowsFormsModuleV2));
            RequiredModuleTypes.Add(typeof(SchedulerWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ScriptRecorderWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(TreeListEditorsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ValidationWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(SystemWindowsFormsModule));
        }

        public override void Setup(XafApplication application){
            base.Setup(application);
            application.Security = new SecurityStrategyComplex(typeof(PermissionPolicyUser),
                typeof(PermissionPolicyRole), new AuthenticationStandard(typeof(PermissionPolicyUser), typeof(AuthenticationStandardLogonParameters)));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            ExtendModel(moduleManager);
#if !NETCOREAPP3_1_OR_GREATER
            moduleManager.ConnectDocumentStyleManager()
                .Merge(moduleManager.ConnectMicrosoftCalendarService())
                .Merge(moduleManager.ConnectMicrosoftService())
                .Merge(moduleManager.ConnectMicrosoftTodoService())
                .Subscribe(this);
#endif
        }

#if !NETCOREAPP3_1_OR_GREATER
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
            => base.GetModuleUpdaters(objectSpace, versionFromDB).Concat(new[]
                {new DocumentStyleManagerModuleUpdater(objectSpace, versionFromDB)});
#endif

        
        private static void ExtendModel(ApplicationModulesManager moduleManager){
            var excludeMaps = new[]
                {PredefinedMap.None, PredefinedMap.LayoutView, PredefinedMap.LayoutViewColumn, PredefinedMap.LabelControl};
            moduleManager.Extend(Enum.GetValues(typeof(PredefinedMap)).OfType<PredefinedMap>()
                .Where(map => !excludeMaps.Contains(map) && map.Platform() == Platform.Win));
            // if (!Debugger.IsAttached){
            moduleManager.Extend(Enum.GetValues(typeof(PredefinedMap)).OfType<PredefinedMap>().Where(map => map==PredefinedMap.ChartControl)
                .Where(map => !excludeMaps.Contains(map) && map.Platform() == Platform.Win));
            // }
        }
    }
}
