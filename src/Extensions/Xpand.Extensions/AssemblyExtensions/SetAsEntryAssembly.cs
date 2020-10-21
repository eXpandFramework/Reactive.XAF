using System;
using System.Reflection;
using Fasterflect;

namespace Xpand.Extensions.AssemblyExtensions{
    public static partial class AssemblyExtensions{
        public static void SetAsEntryAssembly(this Assembly assembly){
            var manager = Assembly.Load("mscorlib").GetType("System.AppDomainManager").CreateInstance();
            var entryAssemblyfield = manager.GetType()
                .GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
            entryAssemblyfield?.SetValue(manager, assembly);
            var domainManagerField = AppDomain.CurrentDomain.GetType().GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
            domainManagerField?.SetValue(AppDomain.CurrentDomain, manager);
        }
    }
}