using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;

namespace Xpand.Extensions.ReflectionExtensions{
    public partial class ReflectionExtensions{
	    private static readonly ModuleBuilder MModule;

        static ReflectionExtensions() 
            => MModule = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("DelegateTypeFactory"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("DelegateTypeFactory");

        public static Type CreateDelegateType(this MethodInfo method){
            var nameBase = $"{method.DeclaringType?.Name}{method.Name}";
            var name = GetUniqueName(nameBase);

            var typeBuilder = MModule.DefineType(
                name, TypeAttributes.Sealed | TypeAttributes.Public, typeof(MulticastDelegate));

            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
                CallingConventions.Standard, new[]{typeof(object), typeof(IntPtr)});
            constructor.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

            var parameters = method.GetParameters();

            var invokeMethod = typeBuilder.DefineMethod(
                "Invoke", MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Public,
                method.ReturnType, parameters.Select(p => p.ParameterType).ToArray());
            invokeMethod.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

            for (var i = 0; i < parameters.Length; i++){
                var parameter = parameters[i];
                invokeMethod.DefineParameter(i + 1, ParameterAttributes.None, parameter.Name);
            }

            return typeBuilder.CreateTypeInfo();
        }

        private static string GetUniqueName(string nameBase){
            var number = 2;
            var name = nameBase;
            while (MModule.GetType(name) != null)
                name = nameBase + number++;
            return name;
        }
    }
}