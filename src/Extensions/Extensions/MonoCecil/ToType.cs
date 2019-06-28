using System;
using Mono.Cecil;

namespace Xpand.Source.Extensions.MonoCecil{
    internal static partial class MonoCecilExtensions{
        public static Type ToType(this TypeReference typeReference){
            string fullName = null;
            if (typeReference.Scope.MetadataScopeType==MetadataScopeType.AssemblyNameReference){
                fullName = ((AssemblyNameReference) typeReference.Scope).FullName;
            }
            else if (typeReference.Scope.MetadataScopeType==MetadataScopeType.ModuleDefinition){
                fullName = ((ModuleDefinition) typeReference.Scope).Assembly.FullName;
            }
            else if (typeReference.Scope.MetadataScopeType==MetadataScopeType.ModuleReference){
                throw new NotImplementedException();
            }

            return Type.GetType($"{typeReference.FullName.Replace("/","+")},{fullName}");
        }

    }
}