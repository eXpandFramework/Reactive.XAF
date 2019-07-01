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
using Xpand.XAF.Modules.Reactive.Extensions;

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

        static PredifinedMapService(){
            Init();
        }

        private static void Init(){
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (ModelExtendingService.Platform==Platform.Win){
                _xafWinAssembly = assemblies.FirstOrDefault(_ => _.FullName.StartsWith("DevExpress.ExpressApp.Win.v"));
                var xtragrid = "DevExpress.XtraGrid.v";
                _gridViewAssembly = assemblies.FirstOrDefault(_ => _.FullName.StartsWith(xtragrid));
                if (_gridViewAssembly == null){
                    _gridViewAssembly=Assembly.LoadFile(Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), $"{xtragrid}*.dll").First());
                }
                _xpandWinAssembly = assemblies.FirstOrDefault(_ => _.FullName.StartsWith("Xpand.ExpressApp.Win"));
            }
            if (ModelExtendingService.Platform==Platform.Web){
                _xafWebAssembly = assemblies.FirstOrDefault(_ => _.FullName.StartsWith("DevExpress.ExpressApp.Web.v"));
                var gridView = "DevExpress.Web.v";
                _dxWebAssembly = assemblies.FirstOrDefault(_ => _.FullName.StartsWith(gridView));
                if (_dxWebAssembly == null){
                    _dxWebAssembly=Assembly.LoadFile(Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), $"{gridView}*.dll").First());
                }
            }
        }

        public static void Extend(this IEnumerable<PredifinedMap> maps, Action<ModelMapperConfiguration> configure = null){
            foreach (var map in maps){
                map.Extend(configure);
            }
        }

        public static void Extend(this PredifinedMap map,Action<ModelMapperConfiguration> configure = null){
            var modelMapperConfiguration = map.ModelMapperConfiguration(configure);
            var result = (modelMapperConfiguration.MapData.typeToMap,modelMapperConfiguration,map);
            (result.typeToMap,result.modelMapperConfiguration).Extend(result.modelMapperConfiguration.MapData.modelType);
        }

        public static IObservable<Type> MapToModel(this IEnumerable<PredifinedMap> configurations,Action<PredifinedMap,ModelMapperConfiguration> configure = null){
            return configurations.Where(_ => _!=PredifinedMap.None)
                .ToObservable()
                .SelectMany(_ => {
                    if (_ == PredifinedMap.BandedGridColumn){
                        return BandedGridColumnService.Connect().Select(unit => _);
                    }

                    return _.AsObservable();
                })
                .Select(_ =>_.ModelMapperConfiguration(configuration => configure?.Invoke(_, configuration))?.MapData.typeToMap)
                .Where(_ => _!=null)
                .MapToModel();
        }

        public static IObservable<Type> MapToModel(this PredifinedMap configuration,Action<ModelMapperConfiguration> configure=null){
            return new[]{ configuration}.MapToModel((mapperConfiguration, modelMapperConfiguration) => configure?.Invoke(modelMapperConfiguration));
        }

        private static ModelMapperConfiguration ModelMapperConfiguration(this PredifinedMap configuration,Action<ModelMapperConfiguration> configure){
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

            if (new[]{PredifinedMap.GridColumn,PredifinedMap.BandedGridColumn,PredifinedMap.LayoutViewColumn}.Any(_ => _==configuration)){
                var viewControl = PredifinedMap.GridView.GetViewControl(view,null);
                var propertyInfo = viewControl.GetType().Properties().Where(info => info.Name == "Columns")
                    .First(info => info.PropertyType.Name.StartsWith(configuration.ToString()));
                return propertyInfo.GetValue(viewControl).GetIndexer(model);
            }
            if (configuration == PredifinedMap.ASPxGridView){
                return ((ListView) view).Editor.GetPropertyValue("Grid");
            }

            if (configuration == PredifinedMap.GridViewColumn){
                return PredifinedMap.ASPxGridView.GetViewControl(view,null).GetPropertyValue("Columns",Flags.InstancePublicDeclaredOnly).GetIndexer(model);
            }

            throw new NotImplementedException(configuration.ToString());
        }

        public static string GetTypeName(this PredifinedMap configuration){
            if (configuration == PredifinedMap.AdvBandedGridView)
                return "DevExpress.XtraGrid.Views.BandedGrid.AdvBandedGridView";
            if (configuration == PredifinedMap.BandedGridColumn)
                return "DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn";
            if (configuration == PredifinedMap.GridView)
                return "DevExpress.XtraGrid.Views.Grid.GridView";
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
                    return GridViewGridColumnConfiguration(predifinedMap,_xafWinAssembly, _gridViewAssembly, "DevExpress.ExpressApp.Win.Editors.GridListEditor",
                        PredifinedMap.GridView.GetTypeName(),PredifinedMap.GridColumn.GetTypeName() );
                }
                if (new[]{PredifinedMap.AdvBandedGridView,PredifinedMap.BandedGridColumn}.Any(map => map==predifinedMap)){
                    return GridViewGridColumnConfiguration(predifinedMap,_xafWinAssembly, _gridViewAssembly, "DevExpress.ExpressApp.Win.Editors.GridListEditor",
                        PredifinedMap.AdvBandedGridView.GetTypeName(), PredifinedMap.BandedGridColumn.GetTypeName());
                }
                if (new[]{PredifinedMap.LayoutView,PredifinedMap.LayoutViewColumn}.Any(map => map==predifinedMap)){
                    return GridViewGridColumnConfiguration(predifinedMap,_xpandWinAssembly, _gridViewAssembly, "Xpand.ExpressApp.Win.ListEditors.GridListEditors.LayoutView.LayoutViewListEditor",
                        PredifinedMap.LayoutView.GetTypeName(), PredifinedMap.LayoutViewColumn.GetTypeName());
                }
            }

            if (ModelExtendingService.Platform==Platform.Web){
                if (new[]{PredifinedMap.ASPxGridView,PredifinedMap.GridViewColumn}.Any(map => map==predifinedMap)){
                    return GridViewGridColumnConfiguration(predifinedMap,_xafWebAssembly, _dxWebAssembly, "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor",
                        PredifinedMap.ASPxGridView.GetTypeName(),PredifinedMap.GridViewColumn.GetTypeName());
                }
            }

            return null;
        }

        private static ModelMapperConfiguration GridViewGridColumnConfiguration(PredifinedMap predifinedMap ,Assembly xafWinAssembly, Assembly gridViewAssembly, string listEditorTypeName, string gridViewTypeName, string gridColumnTypeName){
            if (gridViewAssembly!=null&&xafWinAssembly!=null){
                var rightOperand = xafWinAssembly.GetType(listEditorTypeName);
                if (new[]{PredifinedMap.GridView,PredifinedMap.ASPxGridView,PredifinedMap.AdvBandedGridView, PredifinedMap.LayoutView}.Any(map => map==predifinedMap)){
                    var visibilityCriteria = VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand,"Parent.");
                    var bandsLayout = predifinedMap == PredifinedMap.AdvBandedGridView;
                    visibilityCriteria=CriteriaOperator.Parse($"{visibilityCriteria} AND Parent.BandsLayout.Enable=?", bandsLayout).ToString();
                    var typeToMap=gridViewAssembly.GetType(gridViewTypeName);
                    return new ModelMapperConfiguration {ImageName = "Grid_16x16",VisibilityCriteria =visibilityCriteria,MapData = (typeToMap,typeof(IModelListView))};
                }
                if (new[]{PredifinedMap.GridViewColumn,PredifinedMap.GridColumn, PredifinedMap.BandedGridColumn,PredifinedMap.LayoutViewColumn}.Any(map => map==predifinedMap)){
                    var visibilityCriteria = VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand,"Parent.Parent.Parent.");
                    var bandsLayout = predifinedMap == PredifinedMap.BandedGridColumn;
                    visibilityCriteria=CriteriaOperator.Parse($"{visibilityCriteria} AND Parent.Parent.Parent.BandsLayout.Enable=?", bandsLayout).ToString();
                    var typeToMap=gridViewAssembly.GetType(gridColumnTypeName);
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