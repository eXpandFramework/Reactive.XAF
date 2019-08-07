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
using Fasterflect;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    internal static class TestExtensions{
        internal static IObservable<Type> ModelInterfaces(this IObservable<Type> source){
            return (IObservable<Type>) typeof(TypeMappingService).Methods(Flags.StaticAnyDeclaredOnly,nameof(ModelInterfaces)).First(info => {
                var parameterInfos = info.Parameters();
                return parameterInfos.Count==1&&parameterInfos.First().ParameterType==typeof(IObservable<Type>);
            }).Call(null,source);
        }

        public static IEnumerable<Type> Modules(this PredefinedMap predefinedMap){
            if (predefinedMap == PredefinedMap.DashboardViewer){
                return new[]{typeof(DashboardsModule), typeof(DashboardsWindowsFormsModule)};
            }
            return Enumerable.Empty<Type>();
        }

        public static Assembly Assembly(this PredefinedMap predefinedMap){
            Assembly assembly = null;
            if (new[]{
                PredefinedMap.AdvBandedGridView, PredefinedMap.BandedGridColumn, PredefinedMap.GridView,
                PredefinedMap.GridColumn, PredefinedMap.LayoutView, PredefinedMap.LayoutViewColumn
            }.Contains(predefinedMap))
                assembly = typeof(GridView).Assembly;
            else if (new[]{
                PredefinedMap.RepositoryFieldPicker, PredefinedMap.RepositoryItemRtfEditEx,
                PredefinedMap.RepositoryItemLookupEdit, PredefinedMap.RepositoryItemObjectEdit,
                PredefinedMap.RepositoryItemPopupExpressionEdit, PredefinedMap.RepositoryItemPopupCriteriaEdit,
                PredefinedMap.RepositoryItemProtectedContentTextEdit
            }.Contains(predefinedMap) || predefinedMap == PredefinedMap.XafLayoutControl)
                assembly = typeof(SystemWindowsFormsModule).Assembly;
            else if (predefinedMap.IsRepositoryItem())
                assembly = typeof(RepositoryItem).Assembly;
            else if (predefinedMap == PredefinedMap.SplitContainerControl)
                assembly = typeof(SplitContainerControl).Assembly;
            else if (predefinedMap == PredefinedMap.RichEditControl)
                assembly = typeof(RichEditControl).Assembly;
            else if (predefinedMap == PredefinedMap.LabelControl)
                assembly = typeof(LabelControl).Assembly;
            else if (new[]{
                PredefinedMap.ASPxUploadControl, PredefinedMap.ASPxPopupControl, PredefinedMap.ASPxDateEdit,
                PredefinedMap.ASPxHyperLink, PredefinedMap.ASPxSpinEdit, PredefinedMap.ASPxTokenBox,
                PredefinedMap.ASPxComboBox
            }.Any(map => map == predefinedMap))
                assembly = typeof(ASPxComboBox).Assembly;
            else if (new[]{PredefinedMap.ASPxLookupDropDownEdit, PredefinedMap.ASPxLookupFindEdit}.Any(map =>
                map == predefinedMap))
                assembly = typeof(ASPxLookupDropDownEdit).Assembly;
            else if (new[]{PredefinedMap.DashboardDesigner, PredefinedMap.DashboardViewer}.Any(map =>
                map == predefinedMap))
                assembly = typeof(DashboardDesigner).Assembly;
            else if (new[]{PredefinedMap.ASPxDashboard}.Any(map => map == predefinedMap))
                assembly = typeof(ASPxDashboard).Assembly;
            else if (new[]{PredefinedMap.PivotGridControl, PredefinedMap.PivotGridField}.Contains(predefinedMap))
                assembly = typeof(PivotGridControl).Assembly;
            else if (predefinedMap == PredefinedMap.ChartControl || predefinedMap.IsChartControlDiagram())
                assembly = typeof(ChartControl).Assembly;
            else if (predefinedMap == PredefinedMap.SchedulerControl)
                assembly = typeof(SchedulerControl).Assembly;
            else if (predefinedMap == PredefinedMap.ASPxHtmlEditor)
                assembly = typeof(ASPxHtmlEditor).Assembly;
            else if (predefinedMap == PredefinedMap.ASPxScheduler)
                assembly = typeof(ASPxScheduler).Assembly;
            else if (new[]{PredefinedMap.TreeList, PredefinedMap.TreeListColumn}.Any(map => map == predefinedMap))
                assembly = typeof(TreeList).Assembly;
            if (assembly == null){
                throw new NotImplementedException(predefinedMap.ToString());
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