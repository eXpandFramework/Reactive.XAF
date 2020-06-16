using System.ComponentModel;
using System.Diagnostics;

namespace Xpand.Extensions.ProcessExtensions{
    public static partial class ProcessExtensions{
        public static bool IsUnderIISExpress(this Process currentProcess){
            try{
                if (string.CompareOrdinal(currentProcess.ProcessName, "iisexpress") == 0) return true;
                var parentProcess = currentProcess.Parent();
                return string.CompareOrdinal(parentProcess.ProcessName, "iisexpress") == 0
                       || string.CompareOrdinal(parentProcess.ProcessName, "VSIISExeLauncher") == 0;
            }
            catch (Win32Exception){
                if (!Debugger.IsAttached){
                    return Process.GetCurrentProcess().ProcessName != "iisexpress";
                }

                throw;
            }
        }
    }
}