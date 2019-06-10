using System.Collections.Generic;
using Mono.Cecil;

namespace Xpand.Source.Extensions.MonoCecil{
    internal static partial class MonoCecilExtensions{
        public static IEnumerable<TypeDefinition> BaseClasses(this TypeDefinition klassType){
            for (var typeDefinition = klassType;
                typeDefinition != null;
                typeDefinition = typeDefinition.BaseType?.Resolve()) yield return typeDefinition;
        }

    }
}