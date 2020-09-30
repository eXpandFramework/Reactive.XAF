using System;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Chart.Win;
using DevExpress.ExpressApp.Dashboards.Win;
using DevExpress.ExpressApp.FileAttachments.Win;
using DevExpress.ExpressApp.HtmlPropertyEditor.Win;
using DevExpress.ExpressApp.Notifications.Win;
using DevExpress.ExpressApp.Office.Win;
using DevExpress.ExpressApp.PivotChart.Win;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.ExpressApp.ReportsV2.Win;
using DevExpress.ExpressApp.Scheduler.Win;
using DevExpress.ExpressApp.ScriptRecorder.Win;
using DevExpress.ExpressApp.TreeListEditors.Win;
using DevExpress.ExpressApp.Validation.Win;
using DevExpress.ExpressApp.Win.SystemModule;
using TestApplication.Office.DocumentStyleManager;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.GridListEditor;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.OneView;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Win;

namespace TestApplication.Win{
	public class WinModule : AgnosticModule{
        public WinModule(){
            #region XAF Modules
            RequiredModuleTypes.Add(typeof(ChartWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(DashboardsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(FileAttachmentsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(HtmlPropertyEditorWindowsFormsModule));
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
            // RequiredModuleTypes.Add(typeof(WorkflowWindowsFormsModule));
            #endregion
            RequiredModuleTypes.Add(typeof(GridListEditorModule));
            RequiredModuleTypes.Add(typeof(OneViewModule));
            RequiredModuleTypes.Add(typeof(ReactiveModuleWin));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
	        base.Setup(moduleManager);
	        ExtendModel(moduleManager);
            moduleManager.ConnectDocumentStyleManager()
                .Subscribe(this);
        }

        
        private static void ExtendModel(ApplicationModulesManager moduleManager){
            var excludeMaps = new[]
                {PredefinedMap.None, PredefinedMap.LayoutView, PredefinedMap.LayoutViewColumn, PredefinedMap.LabelControl};
            if (!Debugger.IsAttached){
                moduleManager.Extend(Enum.GetValues(typeof(PredefinedMap)).OfType<PredefinedMap>()
                    .Where(map => !excludeMaps.Contains(map) && map.Platform() == Platform.Win));
            }
        }
    }
}