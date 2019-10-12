using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Xpand.Extensions.Cecil{
    public static partial class MonoCecilExtensions{

        
        public static Type ToType(this TypeReference typeReference,Dictionary<string,Type> typesCache=null){

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
            var key = $"{typeReference.FullName.Replace("/", "+")},{fullName}";
            if (typesCache!=null){
                if (!typesCache.TryGetValue(key, out var type)){
                    type = Type.GetType(key,true);
                    typesCache.Add(key, type);
                }
                return type;
            }

            return Type.GetType(key,true);

        }

    }
}