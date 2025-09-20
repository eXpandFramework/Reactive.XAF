using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Xpand.Extensions.IntPtrExtensions{
    public static partial class IntPtrExtensions{
        public static Process ParentProcess(this IntPtr handle){
            var pbi = new ProcessInformation();
            var status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
            if (status != 0)
                throw new Win32Exception(status);
            return pbi.InheritedFromUniqueProcessId == 0 ? null : Process.GetProcesses()
                    .FirstOrDefault(process => process.Id == pbi.InheritedFromUniqueProcessId.ToInt32());
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
            ref ProcessInformation processInformation, int processInformationLength, out int returnLength);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInformation{
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }
    }
}