﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    public static class PredefinedMapService{
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
        private static Assembly _dashboardWebWebFormsAssembly;
        private static string _dxAssemblyNamePostfix;


        static PredefinedMapService(){
            Init();
        }

        private static void Init(){
            _layoutViewListEditorTypeName = "Xpand.ExpressApp.Win.ListEditors.GridListEditors.LayoutView.LayoutViewListEditor";
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var dxAssembly = assemblies.First(assembly => assembly.FullName.StartsWith("DevExpress"));
            var regex = new Regex(@"\.v[\d]{2}\.[\d]");
            var versionSuffix = regex.Match(dxAssembly.FullName).Value;
            regex = new Regex(",.*");
            _dxAssemblyNamePostfix = regex.Match(dxAssembly.FullName).Value;
            _dxUtilsAssembly = assemblies.GetAssembly($"DevExpress.Utils{versionSuffix}");
            if (ModelExtendingService.Platform == Platform.Win){
                
                _xafWinAssembly = assemblies.GetAssembly($"DevExpress.ExpressApp.Win{versionSuffix}");
                _xafPivotGridWinAssembly = assemblies.GetAssembly($"DevExpress.ExpressApp.PivotGrid.Win{versionSuffix}");
                _xtraRichEditAssembly = assemblies.GetAssembly($"DevExpress.XtraRichEdit{versionSuffix}");
                _xafSchedulerControlAssembly = assemblies.GetAssembly($"DevExpress.ExpressApp.Scheduler.Win{versionSuffix}");
                _xafChartWinAssembly = assemblies.GetAssembly($"DevExpress.ExpressApp.Chart.Win{versionSuffix}");
                _xafTreeListWinAssembly = assemblies.GetAssembly($"DevExpress.ExpressApp.TreeListEditors.Win{versionSuffix}");
                _gridViewAssembly = assemblies.GetAssembly($"DevExpress.XtraGrid{versionSuffix}");
                _schedulerWinAssembly = assemblies.GetAssembly($"DevExpress.XtraScheduler{versionSuffix}");
                _dashboardWinAssembly = assemblies.GetAssembly($"DevExpress.Dashboard{versionSuffix}.Win");
                _dxWinEditorsAssembly = assemblies.GetAssembly($"DevExpress.XtraEditors{versionSuffix}");
                _schedulerCoreAssembly = assemblies.GetAssembly($"DevExpress.XtraScheduler{versionSuffix}.Core");
                _pivotGridControlAssembly = assemblies.GetAssembly($"DevExpress.XtraPivotGrid{versionSuffix}");
                _dxTreeListWinAssembly = assemblies.GetAssembly($"DevExpress.XtraTreeList{versionSuffix}");
                _chartUIControlAssembly = assemblies.GetAssembly($"DevExpress.XtraCharts{versionSuffix}.UI");
                _chartControlAssembly = assemblies.GetAssembly($"DevExpress.XtraCharts{versionSuffix}");
                _chartCoreAssembly = assemblies.GetAssembly($"DevExpress.Charts{versionSuffix}.Core");
                _xpandWinAssembly = assemblies.GetAssembly("Xpand.ExpressApp.Win",true);
            }

            if (ModelExtendingService.Platform == Platform.Web){
                _dashboardWebWebFormsAssembly = assemblies.GetAssembly($"DevExpress.Dashboard{versionSuffix}.Web.WebForms");
                _xafWebAssembly = assemblies.GetAssembly($"DevExpress.ExpressApp.Web{versionSuffix}");
                _dxWebAssembly = assemblies.GetAssembly($"DevExpress.Web{versionSuffix}");
                _dxHtmlEditorWebAssembly = assemblies.GetAssembly($"DevExpress.Web.ASPxHtmlEditor{versionSuffix}");
                _dxScedulerWebAssembly = assemblies.GetAssembly($"DevExpress.Web.ASPxScheduler{versionSuffix}");
                _xafHtmlEditorWebAssembly = assemblies.GetAssembly($"DevExpress.ExpressApp.HtmlPropertyEditor.Web{versionSuffix}");
                _xafSchedulerWebAssembly = assemblies.GetAssembly($"DevExpress.ExpressApp.Scheduler.Web{versionSuffix}");
            }

        }

        private static Assembly GetAssembly(this Assembly[] assemblies,string name,bool partialMatch=false){
            var assembly = assemblies.FirstOrDefault(_ =>!partialMatch?_.GetName().Name==name: _.GetName().Name.StartsWith(name));

            if (assembly == null){
                var wildCard=partialMatch?"*":"";
                var path = Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), $"{name}{wildCard}.dll").FirstOrDefault();
                if (path!=null){
                    return Assembly.LoadFile(path);
                }

                try{
                    return Assembly.Load($"{name}{_dxAssemblyNamePostfix}");
                }
                catch (FileNotFoundException){
                    
                }
            }

            return assembly;
        }

        public static void Extend(this ApplicationModulesManager modulesManager,IEnumerable<PredefinedMap> maps, Action<ModelMapperConfiguration> configure = null){
            foreach (var map in maps){
                modulesManager.Extend(map,configure);
            }
        }

        public static IObservable<(ModelInterfaceExtenders extenders, Type targetInterface)> ExtendMap(this ApplicationModulesManager modulesManager, PredefinedMap map){
            var extendModel = modulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(_ => _.ExtendModel).FirstAsync().Publish().RefCount();
            var typeToMap = map.TypeToMap();
            if (map.IsPropertyEditor() || map.IsRepositoryItem()){
                return extendModel.SelectMany(extenders => TypeMappingService.MappedTypes
                    .Where(_ => {
                        var typeName = $"{_.Attribute<ModelMapLinkAttribute>()?.LinkedTypeName}";
                        return Type.GetType(typeName) == typeToMap;
                    })
                    .Select(type => (extenders, type)))
                    .FirstAsync();

            }

            return modulesManager.ExtendMap(typeToMap);
        }

        public static IObservable<(ModelInterfaceExtenders extenders, Type targetInterface)> ExtendMap(this ApplicationModulesManager modulesManager, Type typeToMap){
            var extendModel = modulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(_ => _.ExtendModel).FirstAsync().Publish().RefCount();
            return extendModel
                .SelectMany(extenders => TypeMappingService.MappedTypes
                    .SelectMany(type => type.ModelMapperContainerTypes()
                        .Where(_ => _.Properties().Any(info => type.IsAssignableFrom(info.PropertyType)))
                        .Where(_ => Type.GetType(_.Attribute<ModelMapLinkAttribute>().LinkedTypeName) == typeToMap)
                        .Select(_ => (extenders, type))))
                .FirstAsync();
        }

        public static void Extend(this ApplicationModulesManager modulesManager,params PredefinedMap[] maps){
            foreach (var map in maps){
                modulesManager.Extend(map);
            }
        }

        public static void Extend(this ApplicationModulesManager modulesManager,Action<PredefinedMap,ModelMapperConfiguration> configure = null, params PredefinedMap[] maps){
            foreach (var map in maps){
                modulesManager.Extend(map,configuration => configure?.Invoke(map, configuration));
            }
        }

        public static void Extend(this ApplicationModulesManager modulesManager, PredefinedMap map,Action<ModelMapperConfiguration> configure = null){
            var modelMapperConfiguration = map.ModelMapperConfiguration(configure);
            var result = (modelMapperConfiguration.TypeToMap, modelMapperConfiguration, map);
            if (map.IsChartControlDiagram()){
                if (map!=PredefinedMap.ChartControlDiagram){
                    map.MapToModel(configuration => {
                        configuration.OmitContainer = true;
                        configure?.Invoke(configuration);
                    }).Wait();
                }
            }
            else if (map.IsRepositoryItem() ){
               
                new[]{map}.MapToModel((predefinedMap, configuration) => {
                    configuration.DisplayName = map.DisplayName();
                    configuration.OmitContainer = true;
                    configure?.Invoke(configuration);
                }).Wait();
                result.modelMapperConfiguration.TypeToMap = typeof(RepositoryItemBaseMap);
                modulesManager.Extend(result.modelMapperConfiguration);
            }
            else if (map.IsPropertyEditor()){
                new[]{map}.MapToModel((predefinedMap, configuration) => {
                    configuration.OmitContainer = true;
                    configure?.Invoke(configuration);
                }).Wait();
                result.modelMapperConfiguration.TypeToMap = typeof(PropertyEditorControlMap);
                modulesManager.Extend(result.modelMapperConfiguration);
            }
            else{
                modulesManager.Extend(result.modelMapperConfiguration);
            }
            
        }

        public static IObservable<Type> MapToModel(this IEnumerable<PredefinedMap> predefinedMaps,Action<PredefinedMap,ModelMapperConfiguration> configure = null){
            var maps = predefinedMaps.Where(_ => _!=PredefinedMap.None).ToArray();
//            if (!maps.Contains(PredefinedMap.RepositoryItem) && maps.Any(map =>map.ToString().StartsWith(PredefinedMap.RepositoryItem.ToString()))){
//                maps = maps.Concat(new[]{PredefinedMap.RepositoryItem}).ToArray();
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

        public static IObservable<Type> MapToModel(this PredefinedMap predefinedMap,Action<ModelMapperConfiguration> configure=null){
            return new[]{ predefinedMap}.MapToModel((mapperConfiguration, modelMapperConfiguration) => configure?.Invoke(modelMapperConfiguration));
        }

        private static ModelMapperConfiguration ModelMapperConfiguration(this PredefinedMap predefinedMap,Action<ModelMapperConfiguration> configure=null){
            var mapperConfiguration = predefinedMap.GetModelMapperConfiguration();
            if (mapperConfiguration != null){
                configure?.Invoke(mapperConfiguration);
                return mapperConfiguration;
            }

            throw new NotImplementedException(predefinedMap.ToString());
        }

        public static IModelNode GetNode(this IModelNode modelNode, PredefinedMap predefinedMap){
            return modelNode.GetNode(predefinedMap.TypeToMap().Name);
        }

        public static object GetViewControl(this PredefinedMap predefinedMap, CompositeView view, string model){
            if (new[]{PredefinedMap.GridView,PredefinedMap.AdvBandedGridView,PredefinedMap.LayoutView}.Any(_ => _==predefinedMap)){
                return ((ListView) view).Editor.Control.GetPropertyValue("MainView");
            }
            if (new[]{PredefinedMap.TreeList}.Any(_ => _==predefinedMap)){
                return ((ListView) view).Editor.GetPropertyValue("TreeList");
            }

            if (new[]{PredefinedMap.PivotGridControl,PredefinedMap.ChartControl,PredefinedMap.SchedulerControl}.Any(_ =>_ == predefinedMap)){
                return ((ListView) view).Editor.GetPropertyValue(predefinedMap.ToString(),Flags.InstancePublic);
            }
            if (predefinedMap.IsChartControlDiagram()){
                return PredefinedMap.ChartControl.GetViewControl(view, model).GetPropertyValue("Diagram");
            }
            if (new[]{PredefinedMap.PivotGridField}.Any(_ =>_ == predefinedMap)){
                return GetColumns(PredefinedMap.PivotGridControl, view, model,"Fields");
            }
            if (new[]{PredefinedMap.GridColumn,PredefinedMap.BandedGridColumn,PredefinedMap.LayoutViewColumn}.Any(_ => _==predefinedMap)){
                return GetColumns(PredefinedMap.GridView, view, model,"Columns");
            }
            if (new[]{PredefinedMap.TreeListColumn}.Any(_ => _==predefinedMap)){
                return GetColumns(PredefinedMap.TreeList, view, model,"Columns");
            }
            if (predefinedMap == PredefinedMap.ASPxGridView){
                return ((ListView) view).Editor.GetPropertyValue("Grid");
            }
            if (predefinedMap == PredefinedMap.GridViewColumn){
                return PredefinedMap.ASPxGridView.GetViewControl(view,null).GetPropertyValue("Columns",Flags.InstancePublicDeclaredOnly).GetIndexer(model);
            }
            if (predefinedMap == PredefinedMap.ASPxScheduler){
                return ((ListView) view).Editor.GetPropertyValue("SchedulerControl");
            }

            if (predefinedMap == PredefinedMap.DashboardDesigner){
                return null;
            }

            if (new[]{PredefinedMap.XafLayoutControl,PredefinedMap.SplitContainerControl}.Any(map => map==predefinedMap)){
                return view.GetPropertyValue(nameof(CompositeView.LayoutManager)).GetPropertyValue(nameof(LayoutManager.Container));
            }

            if (predefinedMap.IsRepositoryItem()){
                object repositoryItem;
                if (view is DetailView){
                    repositoryItem = view.GetItems<PropertyEditor>().First(propertyEditor => propertyEditor.Model.Id==model).Control.GetPropertyValue("Properties");
                }
                else{
                    var column = GetViewControl(PredefinedMap.GridColumn, view, model);
                    repositoryItem = column.GetPropertyValue("ColumnEdit");
                }
                return repositoryItem != null && predefinedMap.TypeToMap().IsInstanceOfType(repositoryItem)? repositoryItem: null;
            }
            if (predefinedMap.IsPropertyEditor()){
                var viewControl = view.GetItems<PropertyEditor>().First(editor => editor.Model.Id == model).Control;
                return predefinedMap.TypeToMap().IsInstanceOfType(viewControl) ? viewControl : null;
            }

            if (predefinedMap == PredefinedMap.ASPxPopupControl){
                return null;
            }
            throw new NotImplementedException(predefinedMap.ToString());
        }

        private static object GetColumns(PredefinedMap container, CompositeView view, string model,string columnsName){
            var viewControl = container.GetViewControl(view, null);
            var columnInfos = viewControl.GetType().GetProperties().Where(info => info.Name == columnsName);
            var propertyInfo = columnInfos.First();
            var propertyName = ((ListView) view).Model.Columns[model].PropertyName;
            return propertyInfo.GetValue(viewControl).GetIndexer(propertyName);
        }

        public static bool IsChartControlDiagram(this PredefinedMap predefinedMap){
            return predefinedMap!=PredefinedMap.ChartControl&& predefinedMap.ToString().StartsWith(PredefinedMap.ChartControl.ToString());
        }

        public static bool IsPropertyEditor(this PredefinedMap predefinedMap){
            return new[] {
                PredefinedMap.DashboardViewer,  PredefinedMap.ASPxComboBox,
                PredefinedMap.ASPxDateEdit, PredefinedMap.ASPxHtmlEditor, PredefinedMap.ASPxHyperLink,
                PredefinedMap.ASPxLookupDropDownEdit, PredefinedMap.ASPxLookupFindEdit, PredefinedMap.ASPxSpinEdit,
                PredefinedMap.ASPxTokenBox, PredefinedMap.ASPxUploadControl, PredefinedMap.ASPxDashboard,PredefinedMap.RichEditControl, 
            }.Any(map => map == predefinedMap);
        }

        public static string ModelTypeName(this PredefinedMap predefinedMap){
            var typeToMap = predefinedMap.TypeToMap();
            return typeToMap.ModelTypeName(typeToMap);
        }

        public static bool IsRepositoryItem(this PredefinedMap predefinedMap){
            return predefinedMap.ToString().StartsWith("Repository");
        }

        public static Assembly TypeToMapAssembly(this PredefinedMap predefinedMap){
            Assembly assembly = null;
            if (new[]{
                PredefinedMap.AdvBandedGridView, PredefinedMap.BandedGridColumn, PredefinedMap.GridView,
                PredefinedMap.GridColumn, PredefinedMap.LayoutView, PredefinedMap.LayoutViewColumn
            }.Contains(predefinedMap)){
                assembly = _gridViewAssembly;
            }
            else if (new[]{
                PredefinedMap.RepositoryFieldPicker, PredefinedMap.RepositoryItemRtfEditEx,
                PredefinedMap.RepositoryItemLookupEdit, PredefinedMap.RepositoryItemObjectEdit,
                PredefinedMap.RepositoryItemPopupExpressionEdit, PredefinedMap.RepositoryItemPopupCriteriaEdit,
                PredefinedMap.RepositoryItemProtectedContentTextEdit, 
            }.Contains(predefinedMap)||predefinedMap==PredefinedMap.XafLayoutControl){
                assembly = _xafWinAssembly;
            }
            else if (predefinedMap.IsRepositoryItem()){
                assembly = _dxWinEditorsAssembly;
            }
            else if (predefinedMap==PredefinedMap.SplitContainerControl){
                assembly = _dxUtilsAssembly;
            }
            else if (predefinedMap==PredefinedMap.RichEditControl){
                assembly = _xtraRichEditAssembly;
            }
            else if (predefinedMap==PredefinedMap.LabelControl){
                assembly = _dxWinEditorsAssembly;
            }
            else if (new[] {
                PredefinedMap.ASPxUploadControl, PredefinedMap.ASPxPopupControl, PredefinedMap.ASPxDateEdit,
                PredefinedMap.ASPxHyperLink, PredefinedMap.ASPxSpinEdit, PredefinedMap.ASPxTokenBox,
                PredefinedMap.ASPxComboBox,PredefinedMap.ASPxGridView, PredefinedMap.GridViewColumn, 
            }.Any(map => map == predefinedMap)){
                assembly = _dxWebAssembly;
            }
            else if (new[]{PredefinedMap.ASPxLookupDropDownEdit,PredefinedMap.ASPxLookupFindEdit, }.Any(map => map==predefinedMap)){
                assembly = _xafWebAssembly;
            }
            else if (new[]{PredefinedMap.DashboardDesigner,PredefinedMap.DashboardViewer}.Any(map => map==predefinedMap)){
                assembly = _dashboardWinAssembly;
            }
            else if (new[]{PredefinedMap.ASPxDashboard}.Any(map => map==predefinedMap)){
                assembly = _dashboardWebWebFormsAssembly;
            }
            else if (new[]{PredefinedMap.PivotGridControl, PredefinedMap.PivotGridField}.Contains(predefinedMap)){
                assembly = _pivotGridControlAssembly;
            }
            else if (predefinedMap.IsChartControlDiagram()){
                assembly = _chartControlAssembly;
            }
            else if (predefinedMap==PredefinedMap.ChartControl){
                assembly = _chartUIControlAssembly;
            }
            else if (predefinedMap==PredefinedMap.SchedulerControl){
                assembly = _schedulerWinAssembly;
            }
            else if (predefinedMap==PredefinedMap.ASPxHtmlEditor){
                assembly = _dxHtmlEditorWebAssembly;
            }
            else if (predefinedMap==PredefinedMap.ASPxScheduler){
                assembly = _dxScedulerWebAssembly;
            }
            else if (new[]{PredefinedMap.TreeList,PredefinedMap.TreeListColumn}.Any(map => map==predefinedMap)){
                assembly = _dxTreeListWinAssembly;
            }

            return assembly;
        }

        public static Type TypeToMap(this PredefinedMap predefinedMap){
            return predefinedMap.TypeToMapAssembly()?.GetType(predefinedMap.GetTypeName());
        }

        public static string DisplayName(this PredefinedMap predefinedMap){
            if (predefinedMap.IsRepositoryItem()){
                if (predefinedMap == PredefinedMap.RepositoryItem){
                    return "Item";
                }
                return predefinedMap.ToString().Replace("Repository", "").Replace("Item", "");
            }

            return predefinedMap.TypeToMap().Name;
        }

        public static string GetTypeName(this PredefinedMap predefinedMap){
            if (predefinedMap == PredefinedMap.AdvBandedGridView)
                return "DevExpress.XtraGrid.Views.BandedGrid.AdvBandedGridView";
            if (predefinedMap == PredefinedMap.BandedGridColumn)
                return "DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn";
            if (predefinedMap == PredefinedMap.GridView)
                return "DevExpress.XtraGrid.Views.Grid.GridView";
            if (predefinedMap == PredefinedMap.RichEditControl)
                return "DevExpress.XtraRichEdit.RichEditControl";
            if (predefinedMap == PredefinedMap.XafLayoutControl)
                return "DevExpress.ExpressApp.Win.Layout.XafLayoutControl";
            if (predefinedMap == PredefinedMap.SplitContainerControl)
                return "DevExpress.XtraEditors.SplitContainerControl";
            if (predefinedMap == PredefinedMap.DashboardDesigner)
                return "DevExpress.DashboardWin.DashboardDesigner";
            if (predefinedMap == PredefinedMap.DashboardViewer)
                return "DevExpress.DashboardWin.DashboardViewer";
            if (predefinedMap == PredefinedMap.ASPxDashboard)
                return "DevExpress.DashboardWeb.ASPxDashboard";
            if (predefinedMap == PredefinedMap.TreeList)
                return "DevExpress.XtraTreeList.TreeList";
            if (predefinedMap == PredefinedMap.TreeListColumn)
                return "DevExpress.XtraTreeList.Columns.TreeListColumn";
            if (predefinedMap == PredefinedMap.RepositoryItem)
                return "DevExpress.XtraEditors.Repository.RepositoryItem";
            if (predefinedMap == PredefinedMap.LabelControl)
                return "DevExpress.XtraEditors.LabelControl";
            if (predefinedMap.IsRepositoryItem()){
                if (predefinedMap == PredefinedMap.RepositoryFieldPicker){
                    return $"DevExpress.ExpressApp.Win.Core.ModelEditor.{predefinedMap}";
                }

                if (new[]{
                    PredefinedMap.RepositoryItemRtfEditEx, PredefinedMap.RepositoryItemLookupEdit, PredefinedMap.RepositoryItemProtectedContentTextEdit,
                    PredefinedMap.RepositoryItemObjectEdit, PredefinedMap.RepositoryItemPopupExpressionEdit,
                    PredefinedMap.RepositoryItemPopupCriteriaEdit
                }.Contains(predefinedMap)) return $"DevExpress.ExpressApp.Win.Editors.{predefinedMap}";
                return $"DevExpress.XtraEditors.Repository.{predefinedMap}";
            }
            if (predefinedMap == PredefinedMap.SchedulerControl)
                return "DevExpress.XtraScheduler.SchedulerControl";
            if (predefinedMap == PredefinedMap.PivotGridControl)
                return "DevExpress.XtraPivotGrid.PivotGridControl";
            if (predefinedMap == PredefinedMap.ChartControl)
                return "DevExpress.XtraCharts.ChartControl";
            if (predefinedMap.IsChartControlDiagram()){
                return $"DevExpress.XtraCharts.{predefinedMap.ToString().Replace(PredefinedMap.ChartControl.ToString(),"")}";
            }
            if (predefinedMap == PredefinedMap.PivotGridField)
                return "DevExpress.XtraPivotGrid.PivotGridField";
            if (predefinedMap == PredefinedMap.GridColumn)
                return "DevExpress.XtraGrid.Columns.GridColumn";
            if (predefinedMap == PredefinedMap.LayoutView)
                return "DevExpress.XtraGrid.Views.Layout.LayoutView";
            if (predefinedMap == PredefinedMap.LayoutViewColumn)
                return "DevExpress.XtraGrid.Columns.LayoutViewColumn";
            if (predefinedMap == PredefinedMap.ASPxGridView)
                return "DevExpress.Web.ASPxGridView";
            if (predefinedMap == PredefinedMap.ASPxUploadControl)
                return "DevExpress.Web.ASPxUploadControl";
            if (predefinedMap == PredefinedMap.ASPxPopupControl)
                return "DevExpress.Web.ASPxPopupControl";
            if (predefinedMap == PredefinedMap.ASPxDateEdit)
                return "DevExpress.Web.ASPxDateEdit";
            if (predefinedMap == PredefinedMap.ASPxHyperLink)
                return "DevExpress.Web.ASPxHyperLink";
            if (predefinedMap == PredefinedMap.ASPxSpinEdit)
                return "DevExpress.Web.ASPxSpinEdit";
            if (predefinedMap == PredefinedMap.ASPxTokenBox)
                return "DevExpress.Web.ASPxTokenBox";
            if (predefinedMap == PredefinedMap.ASPxComboBox)
                return "DevExpress.Web.ASPxComboBox";
            if (predefinedMap == PredefinedMap.ASPxLookupDropDownEdit)
                return "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxLookupDropDownEdit";
            if (predefinedMap == PredefinedMap.ASPxLookupFindEdit)
                return "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxLookupFindEdit";
            if (predefinedMap == PredefinedMap.ASPxScheduler)
                return "DevExpress.Web.ASPxScheduler.ASPxScheduler";
            if (predefinedMap == PredefinedMap.ASPxHtmlEditor)
                return "DevExpress.Web.ASPxHtmlEditor.ASPxHtmlEditor";
            if (predefinedMap == PredefinedMap.GridViewColumn)
                return  "DevExpress.Web.GridViewColumn";
            throw new NotImplementedException(predefinedMap.ToString());
        }

        public static ModelMapperConfiguration GetModelMapperConfiguration(this PredefinedMap predefinedMap){
            if (ModelExtendingService.Platform==Platform.Win){
                if (new[]{PredefinedMap.GridView,PredefinedMap.GridColumn}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafWinAssembly), nameof(_gridViewAssembly));
                    return GetListViewConfiguration(predefinedMap,_xafWinAssembly, _gridViewAssembly, "DevExpress.ExpressApp.Win.Editors.GridListEditor",
                        PredefinedMap.GridView.GetTypeName(),PredefinedMap.GridColumn.GetTypeName() );
                }
                if (new[]{PredefinedMap.TreeList,PredefinedMap.TreeListColumn}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafTreeListWinAssembly), nameof(_dxTreeListWinAssembly));
                    var listViewConfiguration = GetListViewConfiguration(predefinedMap,_xafTreeListWinAssembly, _dxTreeListWinAssembly, "DevExpress.ExpressApp.TreeListEditors.Win.TreeListEditor",
                        PredefinedMap.TreeList.GetTypeName(),PredefinedMap.TreeListColumn.GetTypeName() );
                    if (predefinedMap == PredefinedMap.TreeList){
                        listViewConfiguration.TargetInterfaceTypes.Add(typeof(IModelRootNavigationItems));
                        listViewConfiguration.VisibilityCriteria =
                            $"{VisibilityCriteriaLeftOperand.PropertyExists.GetVisibilityCriteria("EditorType", "Parent")} And {listViewConfiguration.VisibilityCriteria}";
                    }
                    return listViewConfiguration;
                }
                if (new[]{PredefinedMap.SchedulerControl}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafSchedulerControlAssembly), nameof(_schedulerWinAssembly));
                    return GetListViewConfiguration(predefinedMap,_xafSchedulerControlAssembly, _schedulerWinAssembly, "DevExpress.ExpressApp.Scheduler.Win.SchedulerListEditor",
                        PredefinedMap.SchedulerControl.GetTypeName(),null );
                }
                if (new[]{PredefinedMap.PivotGridControl,PredefinedMap.PivotGridField}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafPivotGridWinAssembly), nameof(_pivotGridControlAssembly));
                    return GetListViewConfiguration(predefinedMap,_xafPivotGridWinAssembly, _pivotGridControlAssembly, "DevExpress.ExpressApp.PivotGrid.Win.PivotGridListEditor",
                        PredefinedMap.PivotGridControl.GetTypeName(),PredefinedMap.PivotGridField.GetTypeName() );
                }
                if (new[]{PredefinedMap.ChartControl }.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafChartWinAssembly), nameof(_chartUIControlAssembly));
                    var modelMapperConfiguration = GetListViewConfiguration(predefinedMap,_xafChartWinAssembly, _chartUIControlAssembly, "DevExpress.ExpressApp.Chart.Win.ChartListEditor",
                        PredefinedMap.ChartControl.GetTypeName(),null );
                    var chartListEditorVisibilityCriteria = ListViewVisibilityCriteria(_xafChartWinAssembly.GetType("DevExpress.ExpressApp.Chart.Win.ChartListEditor"));
                    var pivotListEditorVisibilityCriteria = ListViewVisibilityCriteria(_xafPivotGridWinAssembly.GetType("DevExpress.ExpressApp.PivotGrid.Win.PivotGridListEditor"));
                    modelMapperConfiguration.VisibilityCriteria=CriteriaOperator.Parse($"{chartListEditorVisibilityCriteria} OR {pivotListEditorVisibilityCriteria}").ToString();
                    return modelMapperConfiguration;
                }
                if (new[]{PredefinedMap.AdvBandedGridView,PredefinedMap.BandedGridColumn}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafWinAssembly), nameof(_gridViewAssembly));
                    return GetListViewConfiguration(predefinedMap,_xafWinAssembly, _gridViewAssembly, "DevExpress.ExpressApp.Win.Editors.GridListEditor",
                        PredefinedMap.AdvBandedGridView.GetTypeName(), PredefinedMap.BandedGridColumn.GetTypeName());
                }
                if (predefinedMap.IsChartControlDiagram()){
                    CheckRequiredParameters(nameof(_xafChartWinAssembly), nameof(_chartControlAssembly));
                    return GetListViewConfiguration(predefinedMap,_xafChartWinAssembly, _chartControlAssembly, "DevExpress.ExpressApp.Chart.Win.ChartListEditor",
                        PredefinedMap.ChartControl.GetTypeName(), predefinedMap.GetTypeName());
                }
                if (new[]{PredefinedMap.LayoutView,PredefinedMap.LayoutViewColumn}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xpandWinAssembly), nameof(_gridViewAssembly));
                    return GetListViewConfiguration(predefinedMap,_xpandWinAssembly, _gridViewAssembly, _layoutViewListEditorTypeName,
                        PredefinedMap.LayoutView.GetTypeName(), PredefinedMap.LayoutViewColumn.GetTypeName());
                }
                if (new[]{PredefinedMap.XafLayoutControl}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafWinAssembly), nameof(_xafWinAssembly));
                    return new ModelMapperConfiguration(predefinedMap.TypeToMap(), typeof(IModelDetailView)){ImageName = predefinedMap.Attribute<ImageNameAttribute>().ImageName};
                }
                if (new[]{PredefinedMap.SplitContainerControl, }.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_dxWinEditorsAssembly), nameof(_dxWinEditorsAssembly));
                    return new ModelMapperConfiguration(predefinedMap.TypeToMap(), typeof(IModelListViewSplitLayout)){ImageName = predefinedMap.Attribute<ImageNameAttribute>().ImageName};
                }
                if (new[]{PredefinedMap.LabelControl, }.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_dxWinEditorsAssembly), nameof(_dxWinEditorsAssembly));
                    return new ModelMapperConfiguration(predefinedMap.TypeToMap(), typeof(IModelPropertyEditor));
                }
                if (new[]{PredefinedMap.DashboardDesigner,PredefinedMap.DashboardViewer}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_dashboardWinAssembly), nameof(_dashboardWinAssembly));
                    return new ModelMapperConfiguration(predefinedMap.TypeToMap(),typeof(IModelPropertyEditor)){ImageName = predefinedMap.Attribute<ImageNameAttribute>().ImageName};
                }
                if (new[]{PredefinedMap.RichEditControl}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xtraRichEditAssembly), nameof(_xtraRichEditAssembly));
                    return new ModelMapperConfiguration(predefinedMap.TypeToMap(), typeof(IModelPropertyEditor)){ImageName = predefinedMap.Attribute<ImageNameAttribute>().ImageName};
                }

                if (predefinedMap.IsRepositoryItem()){
                    var editorsAssembly = _dxWinEditorsAssembly;
                    var controlAssemblyName = nameof(_dxWinEditorsAssembly);
                    if (new[]{
                        PredefinedMap.RepositoryItemRtfEditEx, PredefinedMap.RepositoryItemLookupEdit,PredefinedMap.RepositoryItemProtectedContentTextEdit, 
                        PredefinedMap.RepositoryItemObjectEdit, PredefinedMap.RepositoryFieldPicker,
                        PredefinedMap.RepositoryItemPopupExpressionEdit, PredefinedMap.RepositoryItemPopupCriteriaEdit
                    }.Contains(predefinedMap)){
                        controlAssemblyName = nameof(_xafWinAssembly);
                        editorsAssembly = _xafWinAssembly;
                    }
                    CheckRequiredParameters(nameof(_xafWinAssembly), controlAssemblyName);
                    var typeToMap = editorsAssembly.GetType(predefinedMap.GetTypeName());
                    if (typeToMap == null){
                        throw new NullReferenceException(predefinedMap.ToString());
                    }
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelPropertyEditor), typeof(IModelColumn));
                }
            }

            if (ModelExtendingService.Platform==Platform.Web){
                if (new[]{PredefinedMap.ASPxDashboard}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_dashboardWebWebFormsAssembly), nameof(_dashboardWebWebFormsAssembly));
                    return new ModelMapperConfiguration(predefinedMap.TypeToMap(),typeof(IModelPropertyEditor));
                }
                if (new[]{PredefinedMap.ASPxGridView,PredefinedMap.GridViewColumn}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafWebAssembly), nameof(_dxWebAssembly));
                    return GetListViewConfiguration(predefinedMap,_xafWebAssembly, _dxWebAssembly, "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor",
                        PredefinedMap.ASPxGridView.GetTypeName(),PredefinedMap.GridViewColumn.GetTypeName());
                }
                if (new[]{PredefinedMap.ASPxScheduler}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafSchedulerWebAssembly), nameof(_dxScedulerWebAssembly));
                    return GetListViewConfiguration(predefinedMap,_xafSchedulerWebAssembly, _dxScedulerWebAssembly, "DevExpress.ExpressApp.Scheduler.Web.ASPxSchedulerListEditor",
                        PredefinedMap.ASPxScheduler.GetTypeName(),null);
                }
                if (new[]{PredefinedMap.ASPxHtmlEditor}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafHtmlEditorWebAssembly), nameof(_dxWebAssembly));
                    var typeToMap = predefinedMap.TypeToMap();
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelPropertyEditor));
                }
                if (new[]{PredefinedMap.ASPxUploadControl}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_dxWebAssembly), nameof(_dxWebAssembly));
                    var typeToMap = predefinedMap.TypeToMap();
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelPropertyEditor));
                }
                if (new[]{PredefinedMap.ASPxPopupControl}.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_dxWebAssembly), nameof(_dxWebAssembly));
                    var typeToMap = predefinedMap.TypeToMap();
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelObjectView),typeof(IModelDashboardView)){ImageName = predefinedMap.Attribute<ImageNameAttribute>().ImageName};
                }
                if (new[]{PredefinedMap.ASPxDateEdit,PredefinedMap.ASPxHyperLink, PredefinedMap.ASPxSpinEdit, PredefinedMap.ASPxTokenBox, PredefinedMap.ASPxComboBox, }.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_dxWebAssembly), nameof(_dxWebAssembly));
                    var typeToMap = predefinedMap.TypeToMap();
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelPropertyEditor));
                }
                if (new[]{PredefinedMap.ASPxLookupDropDownEdit ,PredefinedMap.ASPxLookupFindEdit, }.Any(map => map==predefinedMap)){
                    CheckRequiredParameters(nameof(_xafWebAssembly), nameof(_xafWebAssembly));
                    return new ModelMapperConfiguration(predefinedMap.TypeToMap(), typeof(IModelPropertyEditor));
                }
            }

            return null;
        }

        private static void CheckRequiredParameters(string xafAssemblyName, string controlAssemblyName){
            foreach (var name in new[]{xafAssemblyName,controlAssemblyName}){
                if (typeof(PredefinedMapService).Field(name,Flags.StaticPrivate).GetValue(null) == null){
                    throw new NullReferenceException($"{name} check that {nameof(ModelMapperModule)} is added to the {nameof(ModuleBase.RequiredModuleTypes)} collection");
                }    
            }
        }

        private static ModelMapperConfiguration GetListViewConfiguration(PredefinedMap predefinedMap,
            Assembly listEditorAssembly, Assembly controlAssembly, string listEditorTypeName, string gridViewTypeName,string gridColumnTypeName){
            if (controlAssembly!=null&&listEditorAssembly!=null){
                var rightOperand = listEditorAssembly.GetType(listEditorTypeName);
                if (new[]{PredefinedMap.GridView, PredefinedMap.ASPxGridView, PredefinedMap.AdvBandedGridView,PredefinedMap.TreeList, 
                    PredefinedMap.LayoutView, PredefinedMap.PivotGridControl, PredefinedMap.ChartControl,PredefinedMap.SchedulerControl ,PredefinedMap.ASPxScheduler, 
                }.Any(map => map == predefinedMap)){
                    var visibilityCriteria = ListViewVisibilityCriteria(rightOperand);
                    var bandsLayout = predefinedMap == PredefinedMap.AdvBandedGridView;
                    visibilityCriteria=CriteriaOperator.Parse($"{visibilityCriteria} AND Parent.BandsLayout.Enable=?", bandsLayout).ToString();
                    var typeToMap=controlAssembly.GetType(gridViewTypeName);
                    if (predefinedMap == PredefinedMap.ChartControl){
                        ChartControlService.Connect(typeToMap,_chartCoreAssembly).Subscribe();
                    }
                    if (predefinedMap == PredefinedMap.SchedulerControl){
                        SchedulerControlService.Connect(typeToMap,_schedulerCoreAssembly).Subscribe();
                    }
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelListView)) {ImageName = predefinedMap.Attribute<ImageNameAttribute>().ImageName,VisibilityCriteria =visibilityCriteria};
                }

                if (new[]{PredefinedMap.GridViewColumn, PredefinedMap.GridColumn, PredefinedMap.BandedGridColumn,
                    PredefinedMap.LayoutViewColumn, PredefinedMap.PivotGridField,PredefinedMap.TreeListColumn
                }.Any(map => map == predefinedMap)){
                    var visibilityCriteria = ColumnVisibilityCriteria(rightOperand);
                    var bandsLayout = predefinedMap == PredefinedMap.BandedGridColumn;
                    visibilityCriteria=CriteriaOperator.Parse($"{visibilityCriteria} AND Parent.Parent.Parent.BandsLayout.Enable=?", bandsLayout).ToString();
                    var typeToMap=controlAssembly.GetType(gridColumnTypeName);
                    if (predefinedMap == PredefinedMap.BandedGridColumn){
                        BandedGridColumnService.Connect().Subscribe();
                    }
                    return new ModelMapperConfiguration(typeToMap, typeof(IModelColumn)) {ImageName = predefinedMap.Attribute<ImageNameAttribute>().ImageName,VisibilityCriteria =visibilityCriteria};
                }
                if (predefinedMap.IsChartControlDiagram()){
                    var typeToMap=controlAssembly.GetType(predefinedMap.GetTypeName());
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