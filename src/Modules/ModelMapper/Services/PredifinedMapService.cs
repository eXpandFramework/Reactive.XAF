using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.Predifined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    public static class PredifinedMapService{
        private static Assembly _xafWinAssembly;
        private static Assembly _xafTreeListWinAssembly;
        private static Assembly _dxTreeListWinAssembly;
        private static Assembly _xpandWinAssembly;
        private static Assembly _gridViewAssembly;
        private static Assembly _dxWinEditorsAssembly;
        private static Assembly _xafSchedulerWebAssembly;
        private static Assembly _xafWebAssembly;
        private static Assembly _dxWebAssembly;
        private static Assembly _dxHtmlEditorWebAssembly;
        private static Assembly _xafPivotGridWinAssembly;
        private static Assembly _xafChartWinAssembly;
        private static Assembly _pivotGridControlAssembly;
        private static Assembly _chartUIControlAssembly;
        private static Assembly _chartControlAssembly;
        private static Assembly _chartCoreAssembly;
        private static Assembly _dashboardWinAssembly;
        private static Assembly _xtraRichEditAssembly;
        private static string _layoutViewListEditorTypeName;
        private static Assembly _xafSchedulerControlAssembly;
        private static Assembly _schedulerWinAssembly;
        private static Assembly _schedulerCoreAssembly;
        private static Assembly _xafHtmlEditorWebAssembly;
        private static Assembly _dxScedulerWebAssembly;
        private static Assembly _dxUtilsAssembly;
        private static Assembly _dashboardWebAssembly;


        static PredifinedMapService(){
            Init();
        }

        private static void Init(){
            _layoutViewListEditorTypeName = "Xpand.ExpressApp.Win.ListEditors.GridListEditors.LayoutView.LayoutViewListEditor";
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            _dxUtilsAssembly = assemblies.GetAssembly("DevExpress.Utils.v");
            if (ModelExtendingService.Platform == Platform.Win){
                
                _xafWinAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.Win.v");
                _xafPivotGridWinAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.PivotGrid.Win.v");
                _xtraRichEditAssembly = assemblies.GetAssembly("DevExpress.XtraRichEdit.v");
                _xafSchedulerControlAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.Scheduler.Win.v");
                _xafChartWinAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.Chart.Win.v");
                _xafTreeListWinAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.TreeListEditors.Win.v");
                _gridViewAssembly = assemblies.GetAssembly("DevExpress.XtraGrid.v");
                _schedulerWinAssembly = assemblies.GetAssembly($"DevExpress.XtraScheduler{XafAssemblyInfo.VersionSuffix}",true);
                _dashboardWinAssembly = assemblies.GetAssembly($"DevExpress.Dashboard{XafAssemblyInfo.VersionSuffix}.Win");
                _dxWinEditorsAssembly = assemblies.GetAssembly($"DevExpress.XtraEditors{XafAssemblyInfo.VersionSuffix}",true);
                _schedulerCoreAssembly = assemblies.GetAssembly($"DevExpress.XtraScheduler{XafAssemblyInfo.VersionSuffix}.Core",true);
                _pivotGridControlAssembly = assemblies.GetAssembly("DevExpress.XtraPivotGrid.v");
                _dxTreeListWinAssembly = assemblies.GetAssembly("DevExpress.XtraTreeList.v");
                _chartUIControlAssembly = assemblies.GetAssembly($"DevExpress.XtraCharts{XafAssemblyInfo.VersionSuffix}.UI");
                _chartControlAssembly = assemblies.GetAssembly($"DevExpress.XtraCharts{XafAssemblyInfo.VersionSuffix}",true);
                _chartCoreAssembly = assemblies.GetAssembly($"DevExpress.Charts{XafAssemblyInfo.VersionSuffix}.Core",true);
                _xpandWinAssembly = assemblies.GetAssembly("Xpand.ExpressApp.Win");
            }

            if (ModelExtendingService.Platform == Platform.Web){
                _dashboardWebAssembly = assemblies.GetAssembly($"DevExpress.Dashboard{XafAssemblyInfo.VersionSuffix}.Web");
                _xafWebAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.Web.v");
                _dxWebAssembly = assemblies.GetAssembly("DevExpress.Web.v");
                _dxHtmlEditorWebAssembly = assemblies.GetAssembly("DevExpress.Web.ASPxHtmlEditor.v");
                _dxScedulerWebAssembly = assemblies.GetAssembly("DevExpress.Web.ASPxScheduler.v");
                _xafHtmlEditorWebAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.HtmlPropertyEditor.Web.v");
                _xafSchedulerWebAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.Scheduler.Web.v");
            }

        }

        private static Assembly GetAssembly(this Assembly[] assemblies,string name,bool exactMatch=false){
            var assembly = assemblies.FirstOrDefault(_ =>exactMatch?_.GetName().Name==name: _.GetName().Name.StartsWith(name));

            if (assembly == null){
                var wildCard=exactMatch?"":"*";
                var path = Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), $"{name}{wildCard}.dll").FirstOrDefault();
                if (path!=null){
                    return Assembly.LoadFile(path);
                }
            }
            else
                return assembly;

            return null;
        }

        public static void Extend(this ApplicationModulesManager modulesManager,IEnumerable<PredifinedMap> maps, Action<ModelMapperConfiguration> configure = null){
            foreach (var map in maps){
                modulesManager.Extend(map,configure);
            }
        }

        public static IObservable<(ModelInterfaceExtenders extenders, Type targetInterface)> ExtendMap(this ApplicationModulesManager modulesManager, PredifinedMap map){
            var extendModel = modulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(_ => _.ExtendModel).FirstAsync().Publish().RefCount();
            if (map.IsPropertyEditor() || map.IsRepositoryItem()){
                return extendModel.SelectMany(extenders => TypeMappingService.MappedTypes
                    .Where(_ => {
                        var typeName = $"{_.Attribute<ModelMapLinkAttribute>()?.LinkedTypeName}";
                        return Type.GetType(typeName) == map.TypeToMap();
                    })
                    .Select(type => (extenders, type)))
                    .FirstAsync();

            }
            return extendModel
                .SelectMany(extenders => TypeMappingService.MappedTypes
                    .SelectMany(type => type.ModelMapperContainerTypes()
                        .Where(_ => _.Properties().Any(info => type.IsAssignableFrom(info.PropertyType)))
                        .Where(_ =>_.Attribute<ModelMapLinkAttribute>().LinkedTypeName.StartsWith(map.GetTypeName()))
                        .Select(_ => (extenders,type))))
                .FirstAsync();
        }

        public static void Extend(this ApplicationModulesManager modulesManager,params PredifinedMap[] maps){
            foreach (var map in maps){
                modulesManager.Extend(map);
            }
        }

        public static void Extend(this ApplicationModulesManager modulesManager,Action<PredifinedMap,ModelMapperConfiguration> configure = null, params PredifinedMap[] maps){
            foreach (var map in maps){
                modulesManager.Extend(map,configuration => configure?.Invoke(map, configuration));
            }
        }

        public static void Extend(this ApplicationModulesManager modulesManager, PredifinedMap map,Action<ModelMapperConfiguration> configure = null){
            var modelMapperConfiguration = map.ModelMapperConfiguration(configure);
            var result = (modelMapperConfiguration.TypeToMap, modelMapperConfiguration, map);
            if (map.IsChartControlDiagram()){
                if (map!=PredifinedMap.ChartControlDiagram){
                    map.MapToModel(configure).Wait();
                }
            }
            else if (map.IsRepositoryItem() ){
               
                new[]{map}.MapToModel((predifinedMap, configuration) => {
                    configuration.DisplayName = map.DisplayName();
                    configure?.Invoke(configuration);
                }).Wait();
                result.modelMapperConfiguration.TypeToMap = typeof(RepositoryItemBaseMap);
                modulesManager.Extend(result.modelMapperConfiguration);
            }
            else if (map.IsPropertyEditor()){
                new[]{map}.MapToModel((predifinedMap, configuration) => configure?.Invoke(configuration)).Wait();
                result.modelMapperConfiguration.TypeToMap = typeof(PropertyEditorControlMap);
                modulesManager.Extend(result.modelMapperConfiguration);
            }
            else{
                modulesManager.Extend(result.modelMapperConfiguration);
            }
            
        }

        public static IObservable<Type> MapToModel(this IEnumerable<PredifinedMap> predifinedMaps,Action<PredifinedMap,ModelMapperConfiguration> configure = null){
            var maps = predifinedMaps.Where(_ => _!=PredifinedMap.None).ToArray();
//            if (!maps.Contains(PredifinedMap.RepositoryItem) && maps.Any(map =>map.ToString().StartsWith(PredifinedMap.RepositoryItem.ToString()))){
//                maps = maps.Concat(new[]{PredifinedMap.RepositoryItem}).ToArray();
//            }
            
            var results = maps
                .Select(_ => {
                    var modelMapperConfiguration = _.ModelMapperConfiguration(configuration => configure?.Invoke(_, configuration));
                    return modelMapperConfiguration;
                });
            var repositoryItemResults = maps.Where(map => map.IsRepositoryItem())
                .Select(map => new ModelMapperConfiguration(typeof(RepositoryItemBaseMap), typeof(IModelPropertyEditor), typeof(IModelColumn)))
                .Take(1);
            var propertyEditorControlResults = maps.Where(map => map.IsPropertyEditor())
                .Select(map => new ModelMapperConfiguration(typeof(PropertyEditorControlMap), typeof(IModelPropertyEditor)))
                .Take(1);
            return results.Concat(repositoryItemResults).Concat(propertyEditorControlResults).ToObservable().MapToModel();
        }

        public static IObservable<Type> MapToModel(this PredifinedMap predifinedMap,Action<ModelMapperConfiguration> configure=null){
            return new[]{ predifinedMap}.MapToModel((mapperConfiguration, modelMapperConfiguration) => configure?.Invoke(modelMapperConfiguration));
        }

        private static ModelMapperConfiguration ModelMapperConfiguration(this PredifinedMap predifinedMap,Action<ModelMapperConfiguration> configure=null){
            var mapperConfiguration = predifinedMap.GetModelMapperConfiguration();
            if (mapperConfiguration != null){
                configure?.Invoke(mapperConfiguration);
                return mapperConfiguration;
            }

            throw new NotImplementedException(predifinedMap.ToString());
        }

        public static object GetViewControl(this PredifinedMap predifinedMap, CompositeView view, string model){
            if (new[]{PredifinedMap.GridView,PredifinedMap.AdvBandedGridView,PredifinedMap.LayoutView}.Any(_ => _==predifinedMap)){
                return ((ListView) view).Editor.Control.GetPropertyValue("MainView");
            }
            if (new[]{PredifinedMap.TreeList}.Any(_ => _==predifinedMap)){
                return ((ListView) view).Editor.GetPropertyValue("TreeList");
            }

            if (new[]{PredifinedMap.PivotGridControl,PredifinedMap.ChartControl,PredifinedMap.SchedulerControl}.Any(_ =>_ == predifinedMap)){
                return ((ListView) view).Editor.GetPropertyValue(predifinedMap.ToString(),Flags.InstancePublicDeclaredOnly);
            }
            if (predifinedMap.IsChartControlDiagram()){
                return PredifinedMap.ChartControl.GetViewControl(view, model).GetPropertyValue("Diagram");
            }
            if (new[]{PredifinedMap.PivotGridField}.Any(_ =>_ == predifinedMap)){
                return GetColumns(PredifinedMap.PivotGridControl, predifinedMap, view, model,"Fields");
            }
            if (new[]{PredifinedMap.GridColumn,PredifinedMap.BandedGridColumn,PredifinedMap.LayoutViewColumn}.Any(_ => _==predifinedMap)){
                return GetColumns(PredifinedMap.GridView, predifinedMap, view, model,"Columns");
            }
            if (new[]{PredifinedMap.TreeListColumn}.Any(_ => _==predifinedMap)){
                return GetColumns(PredifinedMap.TreeList, predifinedMap, view, model,"Columns");
            }
            if (predifinedMap == PredifinedMap.ASPxGridView){
                return ((ListView) view).Editor.GetPropertyValue("Grid");
            }
            if (predifinedMap == PredifinedMap.GridViewColumn){
                return PredifinedMap.ASPxGridView.GetViewControl(view,null).GetPropertyValue("Columns",Flags.InstancePublicDeclaredOnly).GetIndexer(model);
            }
            if (predifinedMap == PredifinedMap.ASPxScheduler){
                return ((ListView) view).Editor.GetPropertyValue("SchedulerControl");
            }

            if (predifinedMap == PredifinedMap.DashboardDesigner){
                return null;
            }

            if (new[]{PredifinedMap.XafLayoutControl,PredifinedMap.SplitContainerControl}.Any(map => map==predifinedMap)){
                return view.GetPropertyValue(nameof(CompositeView.LayoutManager)).GetPropertyValue(nameof(LayoutManager.Container));
            }

            if (predifinedMap.IsRepositoryItem()){
                object repositoryItem;
                if (view is DetailView){
                    repositoryItem = view.GetItems<PropertyEditor>().First(propertyEditor => propertyEditor.Model.Id==model).Control.GetPropertyValue("Properties");
                }
                else{
                    var column = GetViewControl(PredifinedMap.GridColumn, view, model);
                    repositoryItem = column.GetPropertyValue("ColumnEdit");
                }
                return repositoryItem != null && predifinedMap.TypeToMap().IsInstanceOfType(repositoryItem)? repositoryItem: null;
            }
            if (predifinedMap.IsPropertyEditor()){
                var viewControl = view.GetItems<PropertyEditor>().First(editor => editor.Model.Id == model).Control;
                return predifinedMap.TypeToMap().IsInstanceOfType(viewControl) ? viewControl : null;
            }

            if (predifinedMap == PredifinedMap.ASPxPopupControl){
                return null;
            }
            throw new NotImplementedException(predifinedMap.ToString());
        }

        private static object GetColumns(PredifinedMap container, PredifinedMap configuration, CompositeView view, string model,string columnsName){
            var viewControl = container.GetViewControl(view, null);
            var columnsInfo = viewControl.GetType().Properties().Where(info => info.Name == columnsName);
            var propertyInfo = columnsInfo.First(info => info.PropertyType.Name.StartsWith(configuration.ToString()));
            var bindingName = view.ObjectTypeInfo.FindMember(model).BindingName;
            return propertyInfo.GetValue(viewControl).GetIndexer(bindingName);
        }

        public static bool IsChartControlDiagram(this PredifinedMap predifinedMap){
            return predifinedMap!=PredifinedMap.ChartControl&& predifinedMap.ToString().StartsWith(PredifinedMap.ChartControl.ToString());
        }

        public static bool IsPropertyEditor(this PredifinedMap predifinedMap){
            return new[] {
                PredifinedMap.DashboardViewer,  PredifinedMap.ASPxComboBox,
                PredifinedMap.ASPxDateEdit, PredifinedMap.ASPxHtmlEditor, PredifinedMap.ASPxHyperLink,
                PredifinedMap.ASPxLookupDropDownEdit, PredifinedMap.ASPxLookupFindEdit, PredifinedMap.ASPxSpinEdit,
                PredifinedMap.ASPxTokenBox, PredifinedMap.ASPxUploadControl, PredifinedMap.ASPxDashboard,PredifinedMap.RichEditControl, 
            }.Any(map => map == predifinedMap);
        }

        public static bool IsRepositoryItem(this PredifinedMap predifinedMap){
            return predifinedMap.ToString().StartsWith("Repository");
        }

        public static Assembly TypeToMapAssembly(this PredifinedMap predifinedMap){
            Assembly assembly = null;
            if (new[]{
                PredifinedMap.AdvBandedGridView, PredifinedMap.BandedGridColumn, PredifinedMap.GridView,
                PredifinedMap.GridColumn, PredifinedMap.LayoutView, PredifinedMap.LayoutViewColumn
            }.Contains(predifinedMap)){
                assembly = _gridViewAssembly;
            }
            else if (new[]{
                PredifinedMap.RepositoryFieldPicker, PredifinedMap.RepositoryItemRtfEditEx,
                PredifinedMap.RepositoryItemLookupEdit, PredifinedMap.RepositoryItemObjectEdit,
                PredifinedMap.RepositoryItemPopupExpressionEdit, PredifinedMap.RepositoryItemPopupCriteriaEdit,
                PredifinedMap.RepositoryItemProtectedContentTextEdit, 
            }.Contains(predifinedMap)||predifinedMap==PredifinedMap.XafLayoutControl){
                assembly = _xafWinAssembly;
            }
            else if (predifinedMap.IsRepositoryItem()){
                assembly = _dxWinEditorsAssembly;
            }
            else if (predifinedMap==PredifinedMap.SplitContainerControl){
                assembly = _dxUtilsAssembly;
            }
            else if (predifinedMap==PredifinedMap.RichEditControl){
                assembly = _xtraRichEditAssembly;
            }
            else if (predifinedMap==PredifinedMap.LabelControl){
                assembly = _dxWinEditorsAssembly;
            }
            else if (new[] {
                PredifinedMap.ASPxUploadControl, PredifinedMap.ASPxPopupControl, PredifinedMap.ASPxDateEdit,
                PredifinedMap.ASPxHyperLink, PredifinedMap.ASPxSpinEdit, PredifinedMap.ASPxTokenBox,
                PredifinedMap.ASPxComboBox
            }.Any(map => map == predifinedMap)){
                assembly = _dxWebAssembly;
            }
            else if (new[]{PredifinedMap.ASPxLookupDropDownEdit,PredifinedMap.ASPxLookupFindEdit, }.Any(map => map==predifinedMap)){
                assembly = _xafWebAssembly;
            }
            else if (new[]{PredifinedMap.DashboardDesigner,PredifinedMap.DashboardViewer}.Any(map => map==predifinedMap)){
                assembly = _dashboardWinAssembly;
            }
            else if (new[]{PredifinedMap.ASPxDashboard}.Any(map => map==predifinedMap)){
                assembly = _dashboardWebAssembly;
            }
            else if (new[]{PredifinedMap.PivotGridControl, PredifinedMap.PivotGridField}.Contains(predifinedMap)){
                assembly = _pivotGridControlAssembly;
            }
            else if (predifinedMap==PredifinedMap.ChartControl||predifinedMap.IsChartControlDiagram()){
                assembly = _chartControlAssembly;
            }
            else if (predifinedMap==PredifinedMap.SchedulerControl){
                assembly = _schedulerWinAssembly;
            }
            else if (predifinedMap==PredifinedMap.ASPxHtmlEditor){
                assembly = _dxHtmlEditorWebAssembly;
            }
            else if (predifinedMap==PredifinedMap.ASPxScheduler){
                assembly = _dxScedulerWebAssembly;
            }
            else if (new[]{PredifinedMap.TreeList,PredifinedMap.TreeListColumn}.Any(map => map==predifinedMap)){
                assembly = _dxTreeListWinAssembly;
            }

            return assembly;
        }

        public static Type TypeToMap(this PredifinedMap predifinedMap){
            return predifinedMap.TypeToMapAssembly()?.GetType(predifinedMap.GetTypeName());
        }

        public static string DisplayName(this PredifinedMap predifinedMap){
            if (predifinedMap.IsRepositoryItem()){
                if (predifinedMap == PredifinedMap.RepositoryItem){
                    return "Item";
                }
                return predifinedMap.ToString().Replace("Repository", "").Replace("Item", "");
            }

            return predifinedMap.TypeToMap().Name;
        }

        public static string GetTypeName(this PredifinedMap predifinedMap){
            if (predifinedMap == PredifinedMap.AdvBandedGridView)
                return "DevExpress.XtraGrid.Views.BandedGrid.AdvBandedGridView";
            if (predifinedMap == PredifinedMap.BandedGridColumn)
                return "DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn";
            if (predifinedMap == PredifinedMap.GridView)
                return "DevExpress.XtraGrid.Views.Grid.GridView";
            if (predifinedMap == PredifinedMap.RichEditControl)
                return "DevExpress.XtraRichEdit.RichEditControl";
            if (predifinedMap == PredifinedMap.XafLayoutControl)
                return "DevExpress.ExpressApp.Win.Layout.XafLayoutControl";
            if (predifinedMap == PredifinedMap.SplitContainerControl)
                return "DevExpress.XtraEditors.SplitContainerControl";
            if (predifinedMap == PredifinedMap.DashboardDesigner)
                return "DevExpress.DashboardWin.DashboardDesigner";
            if (predifinedMap == PredifinedMap.DashboardViewer)
                return "DevExpress.DashboardWin.DashboardViewer";
            if (predifinedMap == PredifinedMap.ASPxDashboard)
                return "DevExpress.DashboardWeb.ASPxDashboard";
            if (predifinedMap == PredifinedMap.TreeList)
                return "DevExpress.XtraTreeList.TreeList";
            if (predifinedMap == PredifinedMap.TreeListColumn)
                return "DevExpress.XtraTreeList.Columns.TreeListColumn";
            if (predifinedMap == PredifinedMap.RepositoryItem)
                return "DevExpress.XtraEditors.Repository.RepositoryItem";
            if (predifinedMap == PredifinedMap.LabelControl)
                return "DevExpress.XtraEditors.LabelControl";
            if (predifinedMap.IsRepositoryItem()){
                if (predifinedMap == PredifinedMap.RepositoryFieldPicker){
                    return $"DevExpress.ExpressApp.Win.Core.ModelEditor.{predifinedMap}";
                }

                if (new[]{
                    PredifinedMap.RepositoryItemRtfEditEx, PredifinedMap.RepositoryItemLookupEdit, PredifinedMap.RepositoryItemProtectedContentTextEdit,
                    PredifinedMap.RepositoryItemObjectEdit, PredifinedMap.RepositoryItemPopupExpressionEdit,
                    PredifinedMap.RepositoryItemPopupCriteriaEdit
                }.Contains(predifinedMap)) return $"DevExpress.ExpressApp.Win.Editors.{predifinedMap}";
                return $"DevExpress.XtraEditors.Repository.{predifinedMap}";
            }
            if (predifinedMap == PredifinedMap.SchedulerControl)
                return "DevExpress.XtraScheduler.SchedulerControl";
            if (predifinedMap == PredifinedMap.PivotGridControl)
                return "DevExpress.XtraPivotGrid.PivotGridControl";
            if (predifinedMap == PredifinedMap.ChartControl)
                return "DevExpress.XtraCharts.ChartControl";
            if (predifinedMap.IsChartControlDiagram()){
                return $"DevExpress.XtraCharts.{predifinedMap.ToString().Replace(PredifinedMap.ChartControl.ToString(),"")}";
            }
            if (predifinedMap == PredifinedMap.PivotGridField)
                return "DevExpress.XtraPivotGrid.PivotGridField";
            if (predifinedMap == PredifinedMap.GridColumn)
                return "DevExpress.XtraGrid.Columns.GridColumn";
            if (predifinedMap == PredifinedMap.LayoutView)
                return "DevExpress.XtraGrid.Views.Layout.LayoutView";
            if (predifinedMap == PredifinedMap.LayoutViewColumn)
                return "DevExpress.XtraGrid.Columns.LayoutViewColumn";
            if (predifinedMap == PredifinedMap.ASPxGridView)
                return "DevExpress.Web.ASPxGridView";
            if (predifinedMap == PredifinedMap.ASPxUploadControl)
                return "DevExpress.Web.ASPxUploadControl";
            if (predifinedMap == PredifinedMap.ASPxPopupControl)
                return "DevExpress.Web.ASPxPopupControl";
            if (predifinedMap == PredifinedMap.ASPxDateEdit)
                return "DevExpress.Web.ASPxDateEdit";
            if (predifinedMap == PredifinedMap.ASPxHyperLink)
                return "DevExpress.Web.ASPxHyperLink";
            if (predifinedMap == PredifinedMap.ASPxSpinEdit)
                return "DevExpress.Web.ASPxSpinEdit";
            if (predifinedMap == PredifinedMap.ASPxTokenBox)
                return "DevExpress.Web.ASPxTokenBox";
            if (predifinedMap == PredifinedMap.ASPxComboBox)
                return "DevExpress.Web.ASPxComboBox";
            if (predifinedMap == PredifinedMap.ASPxLookupDropDownEdit)
                return "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxLookupDropDownEdit";
            if (predifinedMap == PredifinedMap.ASPxLookupFindEdit)
                return "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxLookupFindEdit";
            if (predifinedMap == PredifinedMap.ASPxScheduler)
                return "DevExpress.Web.ASPxScheduler.ASPxScheduler";
            if (predifinedMap == PredifinedMap.ASPxHtmlEditor)
                return "DevExpress.Web.ASPxHtmlEditor.ASPxHtmlEditor";
            if (predifinedMap == PredifinedMap.GridViewColumn)
                return  "DevExpress.Web.GridViewColumn";
            throw new NotImplementedException(predifinedMap.ToString());
        }

        public static ModelMapperConfiguration GetModelMapperConfiguration(this PredifinedMap predifinedMap){
            if (ModelExtendingService.Platform==Platform.Win){
                if (new[]{PredifinedMap.GridView,PredifinedMap.GridColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafWinAssembly), nameof(_gridViewAssembly));
                    return GetListViewConfiguration(predifinedMap,_xafWinAssembly, _gridViewAssembly, "DevExpress.ExpressApp.Win.Editors.GridListEditor",
                        PredifinedMap.GridView.GetTypeName(),PredifinedMap.GridColumn.GetTypeName() );
                }
                if (new[]{PredifinedMap.TreeList,PredifinedMap.TreeListColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafTreeListWinAssembly), nameof(_dxTreeListWinAssembly));
                    var listViewConfiguration = GetListViewConfiguration(predifinedMap,_xafTreeListWinAssembly, _dxTreeListWinAssembly, "DevExpress.ExpressApp.TreeListEditors.Win.TreeListEditor",
                        PredifinedMap.TreeList.GetTypeName(),PredifinedMap.TreeListColumn.GetTypeName() );
                    if (predifinedMap == PredifinedMap.TreeList){
                        listViewConfiguration.TargetInterfaceTypes.Add(typeof(IModelRootNavigationItems));
                        listViewConfiguration.VisibilityCriteria =
                            $"{VisibilityCriteriaLeftOperand.PropertyExists.GetVisibilityCriteria("EditorType", "Parent")} And {listViewConfiguration.VisibilityCriteria}";
                    }
                    return listViewConfiguration;
                }
                if (new[]{PredifinedMap.SchedulerControl}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafSchedulerControlAssembly), nameof(_schedulerWinAssembly));
                    return GetListViewConfiguration(predifinedMap,_xafSchedulerControlAssembly, _schedulerWinAssembly, "DevExpress.ExpressApp.Scheduler.Win.SchedulerListEditor",
                        PredifinedMap.SchedulerControl.GetTypeName(),null );
                }
                if (new[]{PredifinedMap.PivotGridControl,PredifinedMap.PivotGridField}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafPivotGridWinAssembly), nameof(_pivotGridControlAssembly));
                    return GetListViewConfiguration(predifinedMap,_xafPivotGridWinAssembly, _pivotGridControlAssembly, "DevExpress.ExpressApp.PivotGrid.Win.PivotGridListEditor",
                        PredifinedMap.PivotGridControl.GetTypeName(),PredifinedMap.PivotGridField.GetTypeName() );
                }
                if (new[]{PredifinedMap.ChartControl }.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafChartWinAssembly), nameof(_chartUIControlAssembly));
                    var modelMapperConfiguration = GetListViewConfiguration(predifinedMap,_xafChartWinAssembly, _chartUIControlAssembly, "DevExpress.ExpressApp.Chart.Win.ChartListEditor",
                        PredifinedMap.ChartControl.GetTypeName(),null );
                    var chartListEditorVisibilityCriteria = ListViewVisibilityCriteria(_xafChartWinAssembly.GetType("DevExpress.ExpressApp.Chart.Win.ChartListEditor"));
                    var pivotListEditorVisibilityCriteria = ListViewVisibilityCriteria(_xafPivotGridWinAssembly.GetType("DevExpress.ExpressApp.PivotGrid.Win.PivotGridListEditor"));
                    modelMapperConfiguration.VisibilityCriteria=CriteriaOperator.Parse($"{chartListEditorVisibilityCriteria} OR {pivotListEditorVisibilityCriteria}").ToString();
                    return modelMapperConfiguration;
                }
                if (new[]{PredifinedMap.AdvBandedGridView,PredifinedMap.BandedGridColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafWinAssembly), nameof(_gridViewAssembly));
                    return GetListViewConfiguration(predifinedMap,_xafWinAssembly, _gridViewAssembly, "DevExpress.ExpressApp.Win.Editors.GridListEditor",
                        PredifinedMap.AdvBandedGridView.GetTypeName(), PredifinedMap.BandedGridColumn.GetTypeName());
                }
                if (predifinedMap.IsChartControlDiagram()){
                    CheckRequiredParameters(nameof(_xafChartWinAssembly), nameof(_chartControlAssembly));
                    return GetListViewConfiguration(predifinedMap,_xafChartWinAssembly, _chartControlAssembly, "DevExpress.ExpressApp.Chart.Win.ChartListEditor",
                        PredifinedMap.ChartControl.GetTypeName(), predifinedMap.GetTypeName());
                }
                if (new[]{PredifinedMap.LayoutView,PredifinedMap.LayoutViewColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xpandWinAssembly), nameof(_gridViewAssembly));
                    return GetListViewConfiguration(predifinedMap,_xpandWinAssembly, _gridViewAssembly, _layoutViewListEditorTypeName,
                        PredifinedMap.LayoutView.GetTypeName(), PredifinedMap.LayoutViewColumn.GetTypeName());
                }
                if (new[]{PredifinedMap.XafLayoutControl}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xpandWinAssembly), nameof(_xpandWinAssembly));
                    return new ModelMapperConfiguration(predifinedMap.TypeToMap(), typeof(IModelDetailView));
                }
                if (new[]{PredifinedMap.SplitContainerControl, }.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_dxWinEditorsAssembly), nameof(_dxWinEditorsAssembly));
                    return new ModelMapperConfiguration(predifinedMap.TypeToMap(), typeof(IModelListViewSplitLayout));
                }
                if (new[]{PredifinedMap.LabelControl, }.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_dxWinEditorsAssembly), nameof(_dxWinEditorsAssembly));
                    return new ModelMapperConfiguration(predifinedMap.TypeToMap(), typeof(IModelPropertyEditor));
                }
                if (new[]{PredifinedMap.DashboardDesigner,PredifinedMap.DashboardViewer}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_dashboardWinAssembly), nameof(_dashboardWinAssembly));
                    return new ModelMapperConfiguration(predifinedMap.TypeToMap(),typeof(IModelPropertyEditor));
                }
                if (new[]{PredifinedMap.RichEditControl}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xtraRichEditAssembly), nameof(_xtraRichEditAssembly));
                    return new ModelMapperConfiguration(predifinedMap.TypeToMap(), typeof(IModelPropertyEditor));
                }

                if (predifinedMap.IsRepositoryItem()){
                    var editorsAssembly = _dxWinEditorsAssembly;
                    var controlAssemblyName = nameof(_dxWinEditorsAssembly);
                    if (new[]{
                        PredifinedMap.RepositoryItemRtfEditEx, PredifinedMap.RepositoryItemLookupEdit,PredifinedMap.RepositoryItemProtectedContentTextEdit, 
                        PredifinedMap.RepositoryItemObjectEdit, PredifinedMap.RepositoryFieldPicker,
                        PredifinedMap.RepositoryItemPopupExpressionEdit, PredifinedMap.RepositoryItemPopupCriteriaEdit
                    }.Contains(predifinedMap)){
                        controlAssemblyName = nameof(_xafWinAssembly);
                        editorsAssembly = _xafWinAssembly;
                    }
                    CheckRequiredParameters(nameof(_xafWinAssembly), controlAssemblyName);
                    var typeToMap = editorsAssembly.GetType(predifinedMap.GetTypeName());
                    if (typeToMap == null){
                        throw new NullReferenceException(predifinedMap.ToString());
                    }
                    return new ModelMapperConfiguration(typeToMap,new[]{typeof(IModelPropertyEditor),typeof(IModelColumn)});
                }
            }

            if (ModelExtendingService.Platform==Platform.Web){
                if (new[]{PredifinedMap.ASPxDashboard}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_dashboardWebAssembly), nameof(_dashboardWebAssembly));
                    return new ModelMapperConfiguration(predifinedMap.TypeToMap(),typeof(IModelPropertyEditor));
                }
                if (new[]{PredifinedMap.ASPxGridView,PredifinedMap.GridViewColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafWebAssembly), nameof(_dxWebAssembly));
                    return GetListViewConfiguration(predifinedMap,_xafWebAssembly, _dxWebAssembly, "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor",
                        PredifinedMap.ASPxGridView.GetTypeName(),PredifinedMap.GridViewColumn.GetTypeName());
                }
                if (new[]{PredifinedMap.ASPxScheduler}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafSchedulerWebAssembly), nameof(_dxScedulerWebAssembly));
                    return GetListViewConfiguration(predifinedMap,_xafSchedulerWebAssembly, _dxScedulerWebAssembly, "DevExpress.ExpressApp.Scheduler.Web.ASPxSchedulerListEditor",
                        PredifinedMap.ASPxScheduler.GetTypeName(),null);
                }
                if (new[]{PredifinedMap.ASPxHtmlEditor}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafHtmlEditorWebAssembly), nameof(_dxWebAssembly));
                    var typeToMap = predifinedMap.TypeToMap();
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelPropertyEditor));
                }
                if (new[]{PredifinedMap.ASPxUploadControl}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_dxWebAssembly), nameof(_dxWebAssembly));
                    var typeToMap = predifinedMap.TypeToMap();
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelPropertyEditor));
                }
                if (new[]{PredifinedMap.ASPxPopupControl}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_dxWebAssembly), nameof(_dxWebAssembly));
                    var typeToMap = predifinedMap.TypeToMap();
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelView));
                }
                if (new[]{PredifinedMap.ASPxDateEdit,PredifinedMap.ASPxHyperLink, PredifinedMap.ASPxSpinEdit, PredifinedMap.ASPxTokenBox, PredifinedMap.ASPxComboBox, }.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_dxWebAssembly), nameof(_dxWebAssembly));
                    var typeToMap = predifinedMap.TypeToMap();
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelPropertyEditor));
                }
                if (new[]{PredifinedMap.ASPxLookupDropDownEdit ,PredifinedMap.ASPxLookupFindEdit, }.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafWebAssembly), nameof(_xafWebAssembly));
                    return new ModelMapperConfiguration(predifinedMap.TypeToMap(), typeof(IModelPropertyEditor));
                }
            }

            return null;
        }

        private static void CheckRequiredParameters(string xafAssemblyName, string controlAssemblyName){
            foreach (var name in new[]{xafAssemblyName,controlAssemblyName}){
                if (typeof(PredifinedMapService).Field(name,Flags.StaticPrivate).GetValue(null) == null){
                    throw new NullReferenceException($"{name} check that {nameof(ModelMapperModule)} is added to the {nameof(ModuleBase.RequiredModuleTypes)} collection");
                }    
            }
        }

        private static ModelMapperConfiguration GetListViewConfiguration(PredifinedMap predifinedMap,
            Assembly listEditorAssembly, Assembly controlAssembly, string listEditorTypeName, string gridViewTypeName,string gridColumnTypeName){
            if (controlAssembly!=null&&listEditorAssembly!=null){
                var rightOperand = listEditorAssembly.GetType(listEditorTypeName);
                if (new[]{PredifinedMap.GridView, PredifinedMap.ASPxGridView, PredifinedMap.AdvBandedGridView,PredifinedMap.TreeList, 
                    PredifinedMap.LayoutView, PredifinedMap.PivotGridControl, PredifinedMap.ChartControl,PredifinedMap.SchedulerControl ,PredifinedMap.ASPxScheduler, 
                }.Any(map => map == predifinedMap)){
                    var visibilityCriteria = ListViewVisibilityCriteria(rightOperand);
                    var bandsLayout = predifinedMap == PredifinedMap.AdvBandedGridView;
                    visibilityCriteria=CriteriaOperator.Parse($"{visibilityCriteria} AND Parent.BandsLayout.Enable=?", bandsLayout).ToString();
                    var typeToMap=controlAssembly.GetType(gridViewTypeName);
                    if (predifinedMap == PredifinedMap.ChartControl){
                        ChartControlService.Connect(typeToMap,_chartCoreAssembly).Subscribe();
                    }
                    if (predifinedMap == PredifinedMap.SchedulerControl){
                        SchedulerControlService.Connect(typeToMap,_schedulerCoreAssembly).Subscribe();
                    }
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelListView)) {ImageName = "Grid_16x16",VisibilityCriteria =visibilityCriteria};
                }

                if (new[]{PredifinedMap.GridViewColumn, PredifinedMap.GridColumn, PredifinedMap.BandedGridColumn,
                    PredifinedMap.LayoutViewColumn, PredifinedMap.PivotGridField,PredifinedMap.TreeListColumn
                }.Any(map => map == predifinedMap)){
                    var visibilityCriteria = ColumnVisibilityCriteria(rightOperand);
                    var bandsLayout = predifinedMap == PredifinedMap.BandedGridColumn;
                    visibilityCriteria=CriteriaOperator.Parse($"{visibilityCriteria} AND Parent.Parent.Parent.BandsLayout.Enable=?", bandsLayout).ToString();
                    var typeToMap=controlAssembly.GetType(gridColumnTypeName);
                    if (predifinedMap == PredifinedMap.BandedGridColumn){
                        BandedGridColumnService.Connect().Subscribe();
                    }
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelColumn)) {ImageName = @"Office2013\Columns_16x16",VisibilityCriteria =visibilityCriteria};
                }
                if (predifinedMap.IsChartControlDiagram()){
                    var typeToMap=controlAssembly.GetType(predifinedMap.GetTypeName());
                    if (_chartCoreAssembly == null){
                        throw new FileNotFoundException($"DevExpress.Charts{XafAssemblyInfo.VersionSuffix}.Core not found in path");
                    }
                    TypeMappingService.AdditionalReferences.Add(_chartCoreAssembly.GetTypes().First());
                    return new ModelMapperConfiguration (typeToMap);
                }
            }

            return null;
        }

        private static string ListViewVisibilityCriteria(Type rightOperand){
            var visibilityCriteria =VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand,"Parent.");
            return visibilityCriteria;
        }

        private static string ColumnVisibilityCriteria(Type rightOperand){
            var visibilityCriteria =VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand,"Parent.Parent.Parent.");
            return visibilityCriteria;
        }
    }
}