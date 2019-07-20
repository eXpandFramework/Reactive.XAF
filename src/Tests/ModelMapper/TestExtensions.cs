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
        public static IEnumerable<Type> Modules(this PredifinedMap predifinedMap){
            if (predifinedMap == PredifinedMap.DashboardViewer){
                return new Type[]{typeof(DashboardsModule), typeof(DashboardsWindowsFormsModule)};
            }
            return Enumerable.Empty<Type>();
        }

        public static Assembly Assembly(this PredifinedMap predifinedMap){
            Assembly assembly = null;
            if (new[]{
                PredifinedMap.AdvBandedGridView, PredifinedMap.BandedGridColumn, PredifinedMap.GridView,
                PredifinedMap.GridColumn, PredifinedMap.LayoutView, PredifinedMap.LayoutViewColumn
            }.Contains(predifinedMap))
                assembly = typeof(GridView).Assembly;
            else if (new[]{
                PredifinedMap.RepositoryFieldPicker, PredifinedMap.RepositoryItemRtfEditEx,
                PredifinedMap.RepositoryItemLookupEdit, PredifinedMap.RepositoryItemObjectEdit,
                PredifinedMap.RepositoryItemPopupExpressionEdit, PredifinedMap.RepositoryItemPopupCriteriaEdit,
                PredifinedMap.RepositoryItemProtectedContentTextEdit
            }.Contains(predifinedMap) || predifinedMap == PredifinedMap.XafLayoutControl)
                assembly = typeof(SystemWindowsFormsModule).Assembly;
            else if (predifinedMap.IsRepositoryItem())
                assembly = typeof(RepositoryItem).Assembly;
            else if (predifinedMap == PredifinedMap.SplitContainerControl)
                assembly = typeof(SplitContainerControl).Assembly;
            else if (predifinedMap == PredifinedMap.RichEditControl)
                assembly = typeof(RichEditControl).Assembly;
            else if (predifinedMap == PredifinedMap.LabelControl)
                assembly = typeof(LabelControl).Assembly;
            else if (new[]{
                PredifinedMap.ASPxUploadControl, PredifinedMap.ASPxPopupControl, PredifinedMap.ASPxDateEdit,
                PredifinedMap.ASPxHyperLink, PredifinedMap.ASPxSpinEdit, PredifinedMap.ASPxTokenBox,
                PredifinedMap.ASPxComboBox
            }.Any(map => map == predifinedMap))
                assembly = typeof(ASPxComboBox).Assembly;
            else if (new[]{PredifinedMap.ASPxLookupDropDownEdit, PredifinedMap.ASPxLookupFindEdit}.Any(map =>
                map == predifinedMap))
                assembly = typeof(ASPxLookupDropDownEdit).Assembly;
            else if (new[]{PredifinedMap.DashboardDesigner, PredifinedMap.DashboardViewer}.Any(map =>
                map == predifinedMap))
                assembly = typeof(DashboardDesigner).Assembly;
            else if (new[]{PredifinedMap.ASPxDashboard}.Any(map => map == predifinedMap))
                assembly = typeof(ASPxDashboard).Assembly;
            else if (new[]{PredifinedMap.PivotGridControl, PredifinedMap.PivotGridField}.Contains(predifinedMap))
                assembly = typeof(PivotGridControl).Assembly;
            else if (predifinedMap == PredifinedMap.ChartControl || predifinedMap.IsChartControlDiagram())
                assembly = typeof(ChartControl).Assembly;
            else if (predifinedMap == PredifinedMap.SchedulerControl)
                assembly = typeof(SchedulerControl).Assembly;
            else if (predifinedMap == PredifinedMap.ASPxHtmlEditor)
                assembly = typeof(ASPxHtmlEditor).Assembly;
            else if (predifinedMap == PredifinedMap.ASPxScheduler)
                assembly = typeof(ASPxScheduler).Assembly;
            else if (new[]{PredifinedMap.TreeList, PredifinedMap.TreeListColumn}.Any(map => map == predifinedMap))
                assembly = typeof(TreeList).Assembly;
            if (assembly == null){
                throw new NotImplementedException(predifinedMap.ToString());
            }
            return assembly;
        }

        public static ModelMapperTestModule Extend(this PredifinedMap map, ModelMapperTestModule testModule = null,
            Action<ModelMapperConfiguration> configure = null){
            testModule = testModule ?? new ModelMapperTestModule();
            return new[]{map}.Extend(testModule, configure);
        }

        public static ModelMapperTestModule Extend(this PredifinedMap[] maps, ModelMapperTestModule testModule = null,
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