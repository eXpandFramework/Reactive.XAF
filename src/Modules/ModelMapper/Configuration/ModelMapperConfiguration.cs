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
        List<Type> TargetInterfaceTypes { get; }
        Type TypeToMap{ get; set; }
    }



    public class ModelMapperConfiguration : IModelMapperConfiguration{
        public ModelMapperConfiguration(Type typeToMap,params Type[] types){
            TypeToMap = typeToMap;
            TargetInterfaceTypes=new List<Type>(types);
        }

        public string ContainerName{ get; set; }
        public string MapName{ get; set; }
        public string ImageName{ get; set; }
        public List<Type> TargetInterfaceTypes{ get;  }
        public string VisibilityCriteria{ get; set; }
        public Type TypeToMap{ get;  set; }
        public string DisplayName{ get; set; }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode(){
            return $"{ContainerName}{MapName}{ImageName}{VisibilityCriteria}{DisplayName}".GetHashCode();
        }


    }
}