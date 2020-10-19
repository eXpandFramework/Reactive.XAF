using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Chart.Win;
using DevExpress.ExpressApp.Dashboards.Win;
using DevExpress.ExpressApp.FileAttachments.Win;
using DevExpress.ExpressApp.Notifications.Win;
using DevExpress.ExpressApp.Office.Win;
using DevExpress.ExpressApp.PivotChart.Win;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.ExpressApp.ReportsV2.Win;
using DevExpress.ExpressApp.Scheduler.Win;
using DevExpress.ExpressApp.ScriptRecorder.Win;
using DevExpress.ExpressApp.TreeListEditors.Win;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Validation.Win;
using DevExpress.ExpressApp.Win.SystemModule;
using TestApplication.Office.DocumentStyleManager;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.Reactive.Extensions;


namespace TestApplication.Win{
	public class WinModule : AgnosticModule{
        public WinModule(){
            #region XAF Modules
            RequiredModuleTypes.Add(typeof(ChartWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(DashboardsWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(FileAttachmentsWindowsFormsModule));
#if !NETCOREAPP3_1
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
            // RequiredModuleTypes.Add(typeof(WorkflowWindowsFormsModule));
            #endregion

            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.GridListEditor.GridListEditorModule));
#if !NETCOREAPP3_1
            
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.OneView.OneViewModule));
#endif
        }

        public override void Setup(ApplicationModulesManager moduleManager){
	        base.Setup(moduleManager);
            ExtendModel(moduleManager);
            moduleManager.ConnectDocumentStyleManager()
                .Subscribe(this);
        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
            => base.GetModuleUpdaters(objectSpace, versionFromDB).Concat(new[]
                {new DocumentStyleManagerModuleUpdater(objectSpace, versionFromDB)});

        
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