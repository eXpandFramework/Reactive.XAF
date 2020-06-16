using System.Diagnostics;
using Xpand.Extensions.IntPtrExtensions;

namespace Xpand.Extensions.ProcessExtensions{
    public static partial class ProcessExtensions{
        public static Process Parent(this Process process) => process.Handle.ParentProcess();
        
    }
}