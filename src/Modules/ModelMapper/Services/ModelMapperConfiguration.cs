using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

    [Flags]
    public enum PredifinedMap{
        None,
        GridView,
        GridColumn
    }

    public static class PredifinedMapService{
        private static readonly Assembly XAFWinAssembly;
        private static readonly Assembly XtraGridAssembly;

        static PredifinedMapService(){
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (ModelExtendingService.Platform==Platform.Win){
                XAFWinAssembly = assemblies.FirstOrDefault(_ => _.FullName.StartsWith("DevExpress.ExpressApp.Win."));
                var xtragrid = "DevExpress.XtraGrid.";
                XtraGridAssembly = assemblies.FirstOrDefault(_ => _.FullName.StartsWith(xtragrid));
                if (XtraGridAssembly == null){
                    XtraGridAssembly=Assembly.LoadFile(Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), $"{xtragrid}*.dll").First());
                }
            }
        }

        public static void Extend(this PredifinedMap configuration,Action<ModelMapperConfiguration> configure = null){
            var results = FlagEnums.GetFlagMembers(configuration).Select(_ => _.Value)
                .Select(_ => {
                    var modelMapperConfiguration = _.ModelMapperConfiguration(configure);
                    return (modelMapperConfiguration.MapData.typeToMap,modelMapperConfiguration,map:_);
                });
            
            foreach (var result in results){
                (result.typeToMap,result.modelMapperConfiguration).Extend(result.modelMapperConfiguration.MapData.modelType);    
            }
        }

        public static IObservable<Type> MapToModel(this IEnumerable<PredifinedMap> configurations,Action<PredifinedMap,ModelMapperConfiguration> configure = null){
            return configurations.Where(_ => _!=PredifinedMap.None)
                .Select(_ =>_.ModelMapperConfiguration(configuration => configure?.Invoke(_, configuration)).MapData.typeToMap)
                .MapToModel();
        }

        public static IObservable<Type> MapToModel(this PredifinedMap configuration,Action<ModelMapperConfiguration> configure=null){
            return FlagEnums.GetFlagMembers(configuration).Select(_ => _.Value).MapToModel((mapperConfiguration, modelMapperConfiguration) => configure?.Invoke(modelMapperConfiguration));
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
            if (configuration == PredifinedMap.GridView){
                return ((ListView) view).Editor.GetPropertyValue(PredifinedMap.GridView.ToString());
            }
            else if (configuration == PredifinedMap.GridColumn){
                return PredifinedMap.GridView.GetViewControl(view,null).GetPropertyValue("Columns").GetIndexer(model);
            }

            throw new NotImplementedException(configuration.ToString());
        }

        public static ModelMapperConfiguration GetModelMapperConfiguration(this PredifinedMap configuration){
            if (ModelExtendingService.Platform==Platform.Win){
                if (XtraGridAssembly!=null&&XAFWinAssembly!=null){
                    var rightOperand = XAFWinAssembly.GetType("DevExpress.ExpressApp.Win.Editors.GridListEditor");
                    if (configuration == PredifinedMap.GridView){
                        var visibilityCriteria = VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand,"Parent.");
                        var typeToMap=XtraGridAssembly.GetType("DevExpress.XtraGrid.Views.Grid.GridView");
                        return new ModelMapperConfiguration {ImageName = "Grid_16x16",VisibilityCriteria =visibilityCriteria,MapData = (typeToMap,typeof(IModelListView))};
                    }
                    if (configuration == PredifinedMap.GridColumn){
                        var visibilityCriteria = VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand,"Parent.Parent.Parent");
                        var typeToMap=XtraGridAssembly.GetType("DevExpress.XtraGrid.Columns.GridColumn");
                        return new ModelMapperConfiguration {ImageName = @"Office2013\Columns_16x16",VisibilityCriteria =visibilityCriteria,MapData = (typeToMap,typeof(IModelColumn))};
                    }
                }
                
                throw new NotImplementedException();
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