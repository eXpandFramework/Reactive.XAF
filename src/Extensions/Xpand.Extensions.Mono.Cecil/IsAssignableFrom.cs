using System.Linq;
using Mono.Cecil;

namespace Xpand.Extensions.Mono.Cecil{
    public static partial class MonoCecilExtensions{
        public static bool DoesSpecificTypeImplementInterface(this TypeDefinition childTypeDef,TypeDefinition parentInterfaceDef){
            return childTypeDef
                .Interfaces
                .Any(ifaceDef => ifaceDef.InterfaceType.Resolve().DoesSpecificInterfaceImplementInterface( parentInterfaceDef));
        }

        public static bool DoesSpecificInterfaceImplementInterface(this TypeDefinition iface0, TypeDefinition iface1){
            return iface0.MetadataToken == iface1.MetadataToken || iface0.DoesAnySubTypeImplementInterface(iface1);
        }

        public static bool DoesAnySubTypeImplementInterface(this TypeDefinition childType,TypeDefinition parentInterfaceDef){
            return childType
                .BaseClasses()
                .Any(typeDefinition => typeDefinition.DoesSpecificTypeImplementInterface(parentInterfaceDef));
        }
        public static bool IsAssignableFrom(this TypeDefinition target, TypeDefinition source){
            return target == source
                   || target.MetadataToken == source.MetadataToken
                   || source.IsSubclassOf(target)
                   || target.IsInterface && source.DoesAnySubTypeImplementInterface(target);
        }


    }
}