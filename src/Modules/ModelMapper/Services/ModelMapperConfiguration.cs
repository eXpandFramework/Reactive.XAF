using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Model;
using EnumsNET;
using Xpand.Source.Extensions.FunctionOperators;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Services.ObjectMapping;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    public interface IModelMapperConfiguration{
        string VisibilityCriteria{ get; }
        string ContainerName{ get; }
        string MapName{ get; }
        string ImageName{ get; }
    }

    public enum VisibilityCriteriaLeftOperand{
        [Description(IsAssignableFromOperator.OperatorName+ "(Parent."+nameof(IModelListView.EditorType)+",?)")]
        IsAssignableFromModelListVideEditorType
    }

    public static class VisibilityCriteriaLeftOperandService{
        public static string GetVisibilityCriteria(this VisibilityCriteriaLeftOperand leftOperand,object rightOperand){
            if (leftOperand == VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType){
                rightOperand = ((Type) rightOperand).AssemblyQualifiedName;
            }

            return CriteriaOperator.Parse(leftOperand.AsString(EnumFormat.Description), rightOperand).ToString();
        }

    }

    public enum PredifinedModelMapperConfiguration{
        GridView
    }

    public static class PredifinedModelMapperConfigurationService{
        private static readonly Assembly XAFWinAssembly;
        private static readonly Assembly XtraGridAssembly;

        static PredifinedModelMapperConfigurationService(){
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

        public static void Extend(this PredifinedModelMapperConfiguration configuration,Action<ModelMapperConfiguration> configure = null){
            var mapperConfiguration = configuration.ModelMapperConfiguration(configure);
            (mapperConfiguration.MapData.typeToMap,mapperConfiguration).Extend(mapperConfiguration.MapData.modelType);
        }

        public static IObservable<Type> MapToModel(this PredifinedModelMapperConfiguration configuration,Action<ModelMapperConfiguration> configure=null){
            var mapperConfiguration = configuration.ModelMapperConfiguration(configure);
            return mapperConfiguration.MapData.typeToMap.MapToModel(mapperConfiguration);
        }

        private static ModelMapperConfiguration ModelMapperConfiguration(this PredifinedModelMapperConfiguration configuration,Action<ModelMapperConfiguration> configure){
            var mapperConfiguration = configuration.GetModelMapperConfiguration();
            configure?.Invoke(mapperConfiguration);
            return mapperConfiguration;
        }

        public static ModelMapperConfiguration GetModelMapperConfiguration(this PredifinedModelMapperConfiguration configuration){
            if (ModelExtendingService.Platform==Platform.Win){
                if (configuration == PredifinedModelMapperConfiguration.GridView&&XtraGridAssembly!=null&&XAFWinAssembly!=null){
                    var rightOperand = XAFWinAssembly.GetType("DevExpress.ExpressApp.Win.Editors.GridListEditor");
                    var visibilityCriteria = VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType.GetVisibilityCriteria(rightOperand);
                    var typeToMap=XtraGridAssembly.GetType("DevExpress.XtraGrid.Views.Grid.GridView");
                    return new ModelMapperConfiguration {ImageName = "Grid_16x16",VisibilityCriteria =visibilityCriteria,MapData = (typeToMap,typeof(IModelListView))};
                }
            }

            throw new NotImplementedException();
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