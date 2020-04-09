using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Xpand.XAF.Modules.ModelMapper.Configuration{
    public interface IModelMapperConfiguration{
        string VisibilityCriteria{ get; }
        string ContainerName{ get; }
        string MapName{ get; }
        string DisplayName{ get; }
        string ImageName{ get; }
        bool IsDependency{ get; set; }
        List<Type> TargetInterfaceTypes { get; }
        Type TypeToMap{ get; }
        bool OmitContainer{ get; }
    }
    
    public class ModelMapperConfiguration : IModelMapperConfiguration{
        public ModelMapperConfiguration(Type typeToMap,params Type[] targetInterfaceTypes){
            TypeToMap = typeToMap;
            TargetInterfaceTypes=new List<Type>(targetInterfaceTypes);
        }
        public string ContainerName{ get; set; }
        public string MapName{ get; set; }
        public string ImageName{ get; set; }
        public List<Type> TargetInterfaceTypes{ get;  }
        public string VisibilityCriteria{ get; set; }
        public Type TypeToMap{ get;  set; }
        public bool IsDependency{ get;  set; }
        public bool OmitContainer{ get; set; }
        public string DisplayName{ get; set; }
        public override string ToString(){
            return $"TypeToMap:{TypeToMap},ContainerName:{ContainerName},MapName:{MapName},ImageName:{ImageName},VisibilityCriteria:{VisibilityCriteria},DisplayName:{DisplayName}";
        }
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode(){
            return ToString().GetHashCode();
        }
    }
}