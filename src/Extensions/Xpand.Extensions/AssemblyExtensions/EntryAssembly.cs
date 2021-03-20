using System.Reflection;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.AssemblyExtensions {
    public static partial class AssemblyExtensions {
        public static Assembly EntryAssembly { get; set; } = Assembly.GetEntryAssembly();
    }
}