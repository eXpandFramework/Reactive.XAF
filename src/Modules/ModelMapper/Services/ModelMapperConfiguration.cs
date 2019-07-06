using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using EnumsNET;
using Fasterflect;
using Xpand.Source.Extensions.FunctionOperators;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    public interface IModelMapperConfiguration{
        string VisibilityCriteria{ get; }
        string ContainerName{ get; }
        string MapName{ get; }
        string ImageName{ get; }
    }

    public enum VisibilityCriteriaLeftOperand{
        [Description(IsAssignableFromOperator.OperatorName+ "({0}"+nameof(IModelListView.EditorType)+",?)")]
        IsAssignableFromModelListVideEditorType
    }

    public static class VisibilityCriteriaLeftOperandService{
        public static string GetVisibilityCriteria(this VisibilityCriteriaLeftOperand leftOperand,object rightOperand,string path=""){
            if (leftOperand == VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType){
                rightOperand = ((Type) rightOperand).AssemblyQualifiedName;
            }

            var criteria = string.Format(leftOperand.AsString(EnumFormat.Description),path);
            return CriteriaOperator.Parse(criteria, rightOperand).ToString();
        }

    }

    [AttributeUsage(AttributeTargets.Field)]
    public class MapPlatformAttribute:Attribute{
        internal MapPlatformAttribute(Platform platform){
            Platform = platform.ToString();
        }

        public string Platform{ get; }
    }
    public enum PredifinedMap{
        None,

        [MapPlatform(Platform.Win)]
        GridView,
        [MapPlatform(Platform.Win)]
        GridColumn,
        [MapPlatform(Platform.Win)]
        AdvBandedGridView,
        [MapPlatform(Platform.Win)]
        BandedGridColumn,
        [MapPlatform(Platform.Win)]
        LayoutView,
        [MapPlatform(Platform.Win)]
        LayoutViewColumn,
        [MapPlatform(Platform.Win)]
        PivotGridControl,
        [MapPlatform(Platform.Win)]
        PivotGridField,
        [MapPlatform(Platform.Win)]
        ChartControl,
        [MapPlatform(Platform.Web)]
        ASPxGridView,
        [MapPlatform(Platform.Web)]
        GridViewColumn
    }

    public static class PredifinedMapService{
        private static Assembly _xafWinAssembly;
        private static Assembly _xpandWinAssembly;
        private static Assembly _gridViewAssembly;
        private static Assembly _xafWebAssembly;
        private static Assembly _dxWebAssembly;
        private static Assembly _xafPivotGridWinAssembly;
        private static Assembly _xafChartWinAssembly;
        private static Assembly _pivotGridControlAssembly;
        private static Assembly _chartControlAssembly;
        private static string _layoutViewListEditorTypeName;

        static PredifinedMapService(){
            Init();
        }

        private static void Init(){
            _layoutViewListEditorTypeName = "Xpand.ExpressApp.Win.ListEditors.GridListEditors.LayoutView.LayoutViewListEditor";
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (ModelExtendingService.Platform == Platform.Win){
                _xafWinAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.Win.v");
                _xafPivotGridWinAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.PivotGrid.Win.v");
                _xafChartWinAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.Chart.Win.v");
                _gridViewAssembly = assemblies.GetAssembly("DevExpress.XtraGrid.v");
                _pivotGridControlAssembly = assemblies.GetAssembly("DevExpress.XtraPivotGrid.v");
                _chartControlAssembly = assemblies.GetAssembly($"DevExpress.XtraCharts{XafAssemblyInfo.VersionSuffix}.UI");
                _xpandWinAssembly = assemblies.GetAssembly("Xpand.ExpressApp.Win");
            }

            if (ModelExtendingService.Platform == Platform.Web){
                _xafWebAssembly = assemblies.GetAssembly("DevExpress.ExpressApp.Web.v");
                _dxWebAssembly = assemblies.GetAssembly("DevExpress.Web.v");
            }

        }

        private static Assembly GetAssembly(this Assembly[] assemblies,string name,bool exactMatch=false){
            var assembly = assemblies.FirstOrDefault(_ =>exactMatch?_.FullName==name: _.FullName.StartsWith(name));

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

        public static void Extend(this ApplicationModulesManager modulesManager,IEnumerable<PredifinedMap> maps,ApplicationModulesManager applicationModulesManager, Action<ModelMapperConfiguration> configure = null){
            foreach (var map in maps){
                modulesManager.Extend(map,configure);
            }
        }

        public static IObservable<(ModelInterfaceExtenders extenders, Type targetInterface)> ExtendMap(this ApplicationModulesManager modulesManager, PredifinedMap map){
            return modulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(_ => _.ExtendModel).FirstAsync()
                .SelectMany(extenders => TypeMappingService.MappedTypes.Where(type =>typeof(IModelModelMap).IsAssignableFrom(type))
                    .SelectMany(type => type.ModelMapperContainerType()
                        .Where(_ => _.Properties().Any(info => type.IsAssignableFrom(info.PropertyType)))
                        .Where(_ =>_.Attribute<ModelMapLinkAttribute>().LinkedTypeName.StartsWith(map.GetTypeName()))
                        .Select(_ => (extenders,type))))
                .FirstAsync();
        }

        public static void Extend(this ApplicationModulesManager modulesManager, PredifinedMap map,Action<ModelMapperConfiguration> configure = null){
            var modelMapperConfiguration = map.ModelMapperConfiguration(configure);
            var result = (modelMapperConfiguration.MapData.typeToMap, modelMapperConfiguration, map);
            modulesManager.Extend((result.typeToMap, result.modelMapperConfiguration),result.modelMapperConfiguration.MapData.modelType);
        }

        public static IObservable<Type> MapToModel(this IEnumerable<PredifinedMap> configurations,Action<PredifinedMap,ModelMapperConfiguration> configure = null){
            return configurations.Where(_ => _!=PredifinedMap.None)
                .ToObservable()
                .Select(_ =>_.ModelMapperConfiguration(configuration => configure?.Invoke(_, configuration))?.MapData.typeToMap)
                .Where(_ => _!=null)
                .MapToModel();
        }

        public static IObservable<Type> MapToModel(this PredifinedMap configuration,Action<ModelMapperConfiguration> configure=null){
            return new[]{ configuration}.MapToModel((mapperConfiguration, modelMapperConfiguration) => configure?.Invoke(modelMapperConfiguration));
        }

        private static ModelMapperConfiguration ModelMapperConfiguration(this PredifinedMap configuration,Action<ModelMapperConfiguration> configure=null){
            var mapperConfiguration = configuration.GetModelMapperConfiguration();
            if (mapperConfiguration != null){
                configure?.Invoke(mapperConfiguration);
                return mapperConfiguration;
            }

            return null;
        }

        public static object GetViewControl(this PredifinedMap configuration, CompositeView view, string model){
            if (new[]{PredifinedMap.GridView,PredifinedMap.AdvBandedGridView,PredifinedMap.LayoutView}.Any(_ => _==configuration)){
                return ((ListView) view).Editor.Control.GetPropertyValue("MainView");
            }

            if (new[]{PredifinedMap.PivotGridControl,PredifinedMap.ChartControl}.Any(_ =>_ == configuration)){
                return ((ListView) view).Editor.GetPropertyValue(configuration.ToString(),Flags.InstancePublicDeclaredOnly);
            }
            if (new[]{PredifinedMap.PivotGridField}.Any(_ =>_ == configuration)){
                return GetColumns(PredifinedMap.PivotGridControl, configuration, view, model,"Fields");
            }
            if (new[]{PredifinedMap.GridColumn,PredifinedMap.BandedGridColumn,PredifinedMap.LayoutViewColumn}.Any(_ => _==configuration)){
                return GetColumns(PredifinedMap.GridView, configuration, view, model,"Columns");
            }
            if (configuration == PredifinedMap.ASPxGridView){
                return ((ListView) view).Editor.GetPropertyValue("Grid");
            }

            if (configuration == PredifinedMap.GridViewColumn){
                return PredifinedMap.ASPxGridView.GetViewControl(view,null).GetPropertyValue("Columns",Flags.InstancePublicDeclaredOnly).GetIndexer(model);
            }

            throw new NotImplementedException(configuration.ToString());
        }

        private static object GetColumns(PredifinedMap container, PredifinedMap configuration, CompositeView view, string model,string columnsName){
            var viewControl = container.GetViewControl(view, null);
            var columnsInfo = viewControl.GetType().Properties().Where(info => info.Name == columnsName);
            var propertyInfo = columnsInfo.First(info => info.PropertyType.Name.StartsWith(configuration.ToString()));
            var bindingName = view.ObjectTypeInfo.FindMember(model).BindingName;
            return propertyInfo.GetValue(viewControl).GetIndexer(bindingName);
        }

        public static string GetTypeName(this PredifinedMap configuration){
            if (configuration == PredifinedMap.AdvBandedGridView)
                return "DevExpress.XtraGrid.Views.BandedGrid.AdvBandedGridView";
            if (configuration == PredifinedMap.BandedGridColumn)
                return "DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn";
            if (configuration == PredifinedMap.GridView)
                return "DevExpress.XtraGrid.Views.Grid.GridView";
            if (configuration == PredifinedMap.PivotGridControl)
                return "DevExpress.XtraPivotGrid.PivotGridControl";
            if (configuration == PredifinedMap.ChartControl)
                return "DevExpress.XtraCharts.ChartControl";
            if (configuration == PredifinedMap.PivotGridField)
                return "DevExpress.XtraPivotGrid.PivotGridField";
            if (configuration == PredifinedMap.GridColumn)
                return "DevExpress.XtraGrid.Columns.GridColumn";
            if (configuration == PredifinedMap.LayoutView)
                return "DevExpress.XtraGrid.Views.Layout.LayoutView";
            if (configuration == PredifinedMap.LayoutViewColumn)
                return "DevExpress.XtraGrid.Columns.LayoutViewColumn";
            if (configuration == PredifinedMap.ASPxGridView)
                return "DevExpress.Web.ASPxGridView";
            if (configuration == PredifinedMap.GridViewColumn)
                return  "DevExpress.Web.GridViewColumn";
            throw new NotImplementedException();
        }

        public static ModelMapperConfiguration GetModelMapperConfiguration(this PredifinedMap predifinedMap){
            if (ModelExtendingService.Platform==Platform.Win){
                if (new[]{PredifinedMap.GridView,PredifinedMap.GridColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafWinAssembly), nameof(_gridViewAssembly));
                    return GridViewGridColumnConfiguration(predifinedMap,_xafWinAssembly, _gridViewAssembly, "DevExpress.ExpressApp.Win.Editors.GridListEditor",
                        PredifinedMap.GridView.GetTypeName(),PredifinedMap.GridColumn.GetTypeName() );
                }
                if (new[]{PredifinedMap.PivotGridControl,PredifinedMap.PivotGridField}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafPivotGridWinAssembly), nameof(_pivotGridControlAssembly));
                    return GridViewGridColumnConfiguration(predifinedMap,_xafPivotGridWinAssembly, _pivotGridControlAssembly, "DevExpress.ExpressApp.PivotGrid.Win.PivotGridListEditor",
                        PredifinedMap.PivotGridControl.GetTypeName(),PredifinedMap.PivotGridField.GetTypeName() );
                }
                if (new[]{PredifinedMap.ChartControl }.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafChartWinAssembly), nameof(_chartControlAssembly));
                    return GridViewGridColumnConfiguration(predifinedMap,_xafChartWinAssembly, _chartControlAssembly, "DevExpress.ExpressApp.Chart.Win.ChartListEditor",
                        PredifinedMap.ChartControl.GetTypeName(),null );
                }
                if (new[]{PredifinedMap.AdvBandedGridView,PredifinedMap.BandedGridColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafWinAssembly), nameof(_gridViewAssembly));
                    return GridViewGridColumnConfiguration(predifinedMap,_xafWinAssembly, _gridViewAssembly, "DevExpress.ExpressApp.Win.Editors.GridListEditor",
                        PredifinedMap.AdvBandedGridView.GetTypeName(), PredifinedMap.BandedGridColumn.GetTypeName());
                }
                if (new[]{PredifinedMap.LayoutView,PredifinedMap.LayoutViewColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xpandWinAssembly), nameof(_gridViewAssembly));
                    return GridViewGridColumnConfiguration(predifinedMap,_xpandWinAssembly, _gridViewAssembly, _layoutViewListEditorTypeName,
                        PredifinedMap.LayoutView.GetTypeName(), PredifinedMap.LayoutViewColumn.GetTypeName());
                }
            }

            if (ModelExtendingService.Platform==Platform.Web){
                if (new[]{PredifinedMap.ASPxGridView,PredifinedMap.GridViewColumn}.Any(map => map==predifinedMap)){
                    CheckRequiredParameters(nameof(_xafWebAssembly), nameof(_dxWebAssembly));
                    return GridViewGridColumnConfiguration(predifinedMap,_xafWebAssembly, _dxWebAssembly, "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor",
                        PredifinedMap.ASPxGridView.GetTypeName(),PredifinedMap.GridViewColumn.GetTypeName());
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

        private static ModelMapperConfiguration GridViewGridColumnConfiguration(PredifinedMap predifinedMap ,Assembly listEditorAssembly, Assembly gridViewAssembly, string listEditorTypeName, string gridViewTypeName, string gridColumnTypeName){
            if (gridViewAssembly!=null&&listEditorAssembly!=null){
                var rightOperand = listEditorAssembly.GetType(listEditorTypeName);
                if (new[]{PredifinedMap.GridView,PredifinedMap.ASPxGridView,PredifinedMap.AdvBandedGridView, PredifinedMap.LayoutView,PredifinedMap.PivotGridControl,PredifinedMap.ChartControl}.Any(map => map==predifinedMap)){
                    var visibilityCriteria = VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand,"Parent.");
                    var bandsLayout = predifinedMap == PredifinedMap.AdvBandedGridView;
                    visibilityCriteria=CriteriaOperator.Parse($"{visibilityCriteria} AND Parent.BandsLayout.Enable=?", bandsLayout).ToString();
                    var typeToMap=gridViewAssembly.GetType(gridViewTypeName);
                    if (predifinedMap == PredifinedMap.ChartControl){
                        ChartControlSeriesService.Connect().Subscribe();
                    }
                    return new ModelMapperConfiguration {ImageName = "Grid_16x16",VisibilityCriteria =visibilityCriteria,MapData = (typeToMap,typeof(IModelListView))};
                }
                if (new[]{PredifinedMap.GridViewColumn,PredifinedMap.GridColumn, PredifinedMap.BandedGridColumn,PredifinedMap.LayoutViewColumn,PredifinedMap.PivotGridField}.Any(map => map==predifinedMap)){
                    var visibilityCriteria = VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand,"Parent.Parent.Parent.");
                    var bandsLayout = predifinedMap == PredifinedMap.BandedGridColumn;
                    visibilityCriteria=CriteriaOperator.Parse($"{visibilityCriteria} AND Parent.Parent.Parent.BandsLayout.Enable=?", bandsLayout).ToString();
                    var typeToMap=gridViewAssembly.GetType(gridColumnTypeName);
                    if (predifinedMap == PredifinedMap.BandedGridColumn){
                        BandedGridColumnService.Connect().Subscribe();
                    }
                    return new ModelMapperConfiguration {ImageName = @"Office2013\Columns_16x16",VisibilityCriteria =visibilityCriteria,MapData = (typeToMap,typeof(IModelColumn))};
                }
            }

            return null;
        }
    }

    public class ModelMapperConfiguration : IModelMapperConfiguration{
        public string ContainerName{ get; set; }
        public string MapName{ get; set; }
        public string ImageName{ get; set; }
        public string VisibilityCriteria{ get; set; }
        internal (Type typeToMap,Type modelType) MapData{ get; set; }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode(){
            return $"{ContainerName}{MapName}{ImageName}{VisibilityCriteria}".GetHashCode();
        }


    }
}