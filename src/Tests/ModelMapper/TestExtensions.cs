using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.DashboardWeb;
using DevExpress.DashboardWin;
using DevExpress.ExpressApp.Dashboards;
using DevExpress.ExpressApp.Dashboards.Win;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.Web;
using DevExpress.Web.ASPxHtmlEditor;
using DevExpress.Web.ASPxScheduler;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPivotGrid;
using DevExpress.XtraRichEdit;
using DevExpress.XtraScheduler;
using DevExpress.XtraTreeList;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    internal static class TestExtensions{
        public static IEnumerable<Type> Modules(this PredefinedMap PredefinedMap){
            if (PredefinedMap == PredefinedMap.DashboardViewer){
                return new Type[]{typeof(DashboardsModule), typeof(DashboardsWindowsFormsModule)};
            }
            return Enumerable.Empty<Type>();
        }

        public static Assembly Assembly(this PredefinedMap PredefinedMap){
            Assembly assembly = null;
            if (new[]{
                PredefinedMap.AdvBandedGridView, PredefinedMap.BandedGridColumn, PredefinedMap.GridView,
                PredefinedMap.GridColumn, PredefinedMap.LayoutView, PredefinedMap.LayoutViewColumn
            }.Contains(PredefinedMap))
                assembly = typeof(GridView).Assembly;
            else if (new[]{
                PredefinedMap.RepositoryFieldPicker, PredefinedMap.RepositoryItemRtfEditEx,
                PredefinedMap.RepositoryItemLookupEdit, PredefinedMap.RepositoryItemObjectEdit,
                PredefinedMap.RepositoryItemPopupExpressionEdit, PredefinedMap.RepositoryItemPopupCriteriaEdit,
                PredefinedMap.RepositoryItemProtectedContentTextEdit
            }.Contains(PredefinedMap) || PredefinedMap == PredefinedMap.XafLayoutControl)
                assembly = typeof(SystemWindowsFormsModule).Assembly;
            else if (PredefinedMap.IsRepositoryItem())
                assembly = typeof(RepositoryItem).Assembly;
            else if (PredefinedMap == PredefinedMap.SplitContainerControl)
                assembly = typeof(SplitContainerControl).Assembly;
            else if (PredefinedMap == PredefinedMap.RichEditControl)
                assembly = typeof(RichEditControl).Assembly;
            else if (PredefinedMap == PredefinedMap.LabelControl)
                assembly = typeof(LabelControl).Assembly;
            else if (new[]{
                PredefinedMap.ASPxUploadControl, PredefinedMap.ASPxPopupControl, PredefinedMap.ASPxDateEdit,
                PredefinedMap.ASPxHyperLink, PredefinedMap.ASPxSpinEdit, PredefinedMap.ASPxTokenBox,
                PredefinedMap.ASPxComboBox
            }.Any(map => map == PredefinedMap))
                assembly = typeof(ASPxComboBox).Assembly;
            else if (new[]{PredefinedMap.ASPxLookupDropDownEdit, PredefinedMap.ASPxLookupFindEdit}.Any(map =>
                map == PredefinedMap))
                assembly = typeof(ASPxLookupDropDownEdit).Assembly;
            else if (new[]{PredefinedMap.DashboardDesigner, PredefinedMap.DashboardViewer}.Any(map =>
                map == PredefinedMap))
                assembly = typeof(DashboardDesigner).Assembly;
            else if (new[]{PredefinedMap.ASPxDashboard}.Any(map => map == PredefinedMap))
                assembly = typeof(ASPxDashboard).Assembly;
            else if (new[]{PredefinedMap.PivotGridControl, PredefinedMap.PivotGridField}.Contains(PredefinedMap))
                assembly = typeof(PivotGridControl).Assembly;
            else if (PredefinedMap == PredefinedMap.ChartControl || PredefinedMap.IsChartControlDiagram())
                assembly = typeof(ChartControl).Assembly;
            else if (PredefinedMap == PredefinedMap.SchedulerControl)
                assembly = typeof(SchedulerControl).Assembly;
            else if (PredefinedMap == PredefinedMap.ASPxHtmlEditor)
                assembly = typeof(ASPxHtmlEditor).Assembly;
            else if (PredefinedMap == PredefinedMap.ASPxScheduler)
                assembly = typeof(ASPxScheduler).Assembly;
            else if (new[]{PredefinedMap.TreeList, PredefinedMap.TreeListColumn}.Any(map => map == PredefinedMap))
                assembly = typeof(TreeList).Assembly;
            if (assembly == null){
                throw new NotImplementedException(PredefinedMap.ToString());
            }
            return assembly;
        }

        public static ModelMapperTestModule Extend(this PredefinedMap map, ModelMapperTestModule testModule = null,
            Action<ModelMapperConfiguration> configure = null){
            testModule = testModule ?? new ModelMapperTestModule();
            return new[]{map}.Extend(testModule, configure);
        }

        public static ModelMapperTestModule Extend(this PredefinedMap[] maps, ModelMapperTestModule testModule = null,
            Action<ModelMapperConfiguration> configure = null){
            testModule = testModule ?? new ModelMapperTestModule();
            testModule.ApplicationModulesManager.FirstAsync()
                .SelectMany(manager => maps.Select(map => {
                    manager.Extend(map, configure);
                    return Unit.Default;
                }))
                .Subscribe();
            return testModule;
        }

        public static ModelMapperTestModule Extend(this Type extenderType,
            Action<ModelMapperConfiguration> configure, ModelMapperTestModule testModule = null){
            testModule = testModule ?? new ModelMapperTestModule();
            testModule.ApplicationModulesManager.FirstAsync()
                .Do(manager => {
                    var configuration = new ModelMapperConfiguration(extenderType);
                    configure?.Invoke(configuration);
                    manager.Extend(configuration);
                })
                .Subscribe();
            return testModule;
        }

        public static ModelMapperTestModule Extend<T>(this Type extenderType, ModelMapperTestModule testModule = null,
            Action<ModelMapperConfiguration> configure = null) where T : IModelNode{
            testModule = testModule ?? new ModelMapperTestModule();
            testModule.ApplicationModulesManager.FirstAsync()
                .Do(manager => {
                    var configuration = new ModelMapperConfiguration(extenderType, typeof(T));
                    configure?.Invoke(configuration);
                    manager.Extend(configuration);
                })
                .Subscribe();
            return testModule;
        }
    }
}