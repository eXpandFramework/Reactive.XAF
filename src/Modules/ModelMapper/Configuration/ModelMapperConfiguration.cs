using System;
using System.Diagnostics.CodeAnalysis;

namespace Xpand.XAF.Modules.ModelMapper.Configuration{
    public interface IModelMapperConfiguration{
        string VisibilityCriteria{ get; }
        string ContainerName{ get; }
        string MapName{ get; }
        string DisplayName{ get; }
        string ImageName{ get; }
    }



    public class ModelMapperConfiguration : IModelMapperConfiguration{
        public string ContainerName{ get; set; }
        public string MapName{ get; set; }
        public string ImageName{ get; set; }
        public string VisibilityCriteria{ get; set; }
        internal (Type typeToMap,Type[] targetInterfaceTypes) MapData{ get; set; }
        public string DisplayName{ get; set; }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode(){
            return $"{ContainerName}{MapName}{ImageName}{VisibilityCriteria}{DisplayName}".GetHashCode();
        }


    }
}