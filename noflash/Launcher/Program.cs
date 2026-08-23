using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Launcher;

class Program
{
    const uint CREATE_SUSPENDED = 0x00000004;
    const uint MEM_COMMIT = 0x1000;
    const uint MEM_RESERVE = 0x2000;
    const uint MEM_RELEASE = 0x8000;
    const uint PAGE_EXECUTE_READWRITE = 0x40;
    const uint PAGE_EXECUTE_READ = 0x20;
    const uint PAGE_READWRITE = 0x04;
    const uint INFINITE = 0xFFFFFFFF;

    static int Main(string[] args)
    {
        var exePath = @"C:\nvm4w\nodejs\node_modules\lean-ctx-bin\bin\lean-ctx.exe";
        if (!File.Exists(exePath))
        { Console.Error.WriteLine("lean-ctx.exe not found"); return 1; }

        var cmdLine = $"\"{exePath}\"";
        if (args.Length > 0)
            cmdLine += " " + string.Join(" ", args.Select(EscapeArg));

        var si = new STARTUPINFOW();
        si.cb = (uint)Marshal.SizeOf<STARTUPINFOW>();
        var pi = new PROCESS_INFORMATION();

        if (!CreateProcessW(null, cmdLine, IntPtr.Zero, IntPtr.Zero, true,
                CREATE_SUSPENDED, IntPtr.Zero, null, ref si, out pi))
        { Console.Error.WriteLine($"CreateProcess failed: {Marshal.GetLastWin32Error()}"); return 1; }

        InstallHook(pi.hProcess);

        ResumeThread(pi.hThread);
        CloseHandle(pi.hThread);
        WaitForSingleObject(pi.hProcess, INFINITE);
        GetExitCodeProcess(pi.hProcess, out uint ec);
        CloseHandle(pi.hProcess);
        return (int)ec;
    }

    static unsafe void InstallHook(IntPtr hProcess)
    {
        // Get CreateProcessW address from our process (same in target — known DLL)
        var kernel32 = GetModuleHandle("kernel32.dll");
        var cpwAddr = GetProcAddress(kernel32, "CreateProcessW");
        if (cpwAddr == IntPtr.Zero) return;

        // Read original bytes from our own process (19 bytes = 4 complete instructions)
        var cpwPtr = (byte*)cpwAddr;
        var origBytes = new byte[19];
        for (int i = 0; i < 19; i++) origBytes[i] = cpwPtr[i];

        // Build the hook stub: modify dwCreationFlags, execute original prologue, JMP back
        var stub = new byte[]
        {
            0x8B, 0x44, 0x24, 0x30,       // mov eax, [rsp+0x30]  (dwCreationFlags)
            0x0D, 0x00, 0x00, 0x00, 0x08, // or eax, CREATE_NO_WINDOW
            0x25, 0xEF, 0xFF, 0xFF, 0xFF, // and eax, ~CREATE_NEW_CONSOLE
            0x89, 0x44, 0x24, 0x30,       // mov [rsp+0x30], eax
        };

        const int origLen = 19;
        int totalLen = stub.Length + origLen + 14;
        var fullStub = new byte[totalLen];
        Array.Copy(stub, 0, fullStub, 0, stub.Length);
        Array.Copy(origBytes, 0, fullStub, stub.Length, origLen);
        int jmpOff = stub.Length + origLen;
        fullStub[jmpOff] = 0xFF; fullStub[jmpOff+1] = 0x25;
        BitConverter.GetBytes((long)cpwAddr + origLen).CopyTo(fullStub, jmpOff + 6);

        // Allocate stub in target
        var stubAddr = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)totalLen,
            MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (stubAddr == IntPtr.Zero) return;
        fixed (byte* p = fullStub)
            WriteProcessMemory(hProcess, stubAddr, (IntPtr)p, (uint)totalLen, out _);

        // Build JMP patch
        var patch = new byte[14];
        patch[0] = 0xFF; patch[1] = 0x25;
        BitConverter.GetBytes((long)stubAddr).CopyTo(patch, 6);

        // Call VirtualProtect from INSIDE the target process via remote thread
        // This avoids the cross-process VirtualProtectEx failure
        var vpAddr = GetProcAddress(kernel32, "VirtualProtect");

        // x64 shellcode: VirtualProtect(cpwAddr, 14, PAGE_EXECUTE_READWRITE, &old)
        // Signature: BOOL VirtualProtect(LPVOID addr, SIZE_T size, DWORD prot, PDWORD old)
        // Thread entry: DWORD ThreadFunc(LPVOID param) — param = pointer to args struct
        var args = new byte[8 + 8 + 4 + 4 + 4]; // addr(8) + size(8) + prot(4) + old(4) + padding
        BitConverter.GetBytes((long)cpwAddr).CopyTo(args, 0);
        BitConverter.GetBytes(14ul).CopyTo(args, 8);
        BitConverter.GetBytes(PAGE_EXECUTE_READWRITE).CopyTo(args, 12);

        var argsAddr = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)args.Length,
            MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (argsAddr == IntPtr.Zero) return;
        fixed (byte* p = args)
            WriteProcessMemory(hProcess, argsAddr, (IntPtr)p, (uint)args.Length, out _);

        // Shellcode: read args from param, call VirtualProtect, return
        // RCX = argsAddr (lpParam from CreateRemoteThread)
        // args struct layout: [rcx+0]=addr, [rcx+8]=size, [rcx+16]=prot, [rcx+20]=&old
        var vpShell = new List<byte>();
        vpShell.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x38 });      // sub rsp, 56 (align 16 + shadow)
        vpShell.AddRange(new byte[] { 0x48, 0x8B, 0x09 });            // mov rcx, [rcx]       — addr
        vpShell.AddRange(new byte[] { 0x48, 0x8B, 0x51, 0xF8 });      // mov rdx, [rcx-8]     — this is wrong...
        
        // Actually, args are laid out at RCX. Let me use the correct offset.
        // argsAddr points to: [addr(8)][size(8)][prot(4)][old(4)]
        // RCX = argsAddr
        // VirtualProtect(params): RCX=addr, RDX=size, R8D=prot, R9=oldOutPtr
        // So we need: mov rcx, [rcx]; mov rdx, [rcx+8]; mov r8d, [rcx+16]; lea r9, [rcx+20]
        vpShell.Clear();
        // push rbx for alignment
        vpShell.Add(0x53);                                             // push rbx
        vpShell.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x30 });      // sub rsp, 48 (shadow + align)
        vpShell.AddRange(new byte[] { 0x48, 0x8B, 0x19 });            // mov rbx, rcx (save args pointer)
        vpShell.AddRange(new byte[] { 0x48, 0x8B, 0x0B });            // mov rcx, [rbx]       — addr
        vpShell.AddRange(new byte[] { 0x48, 0x8B, 0x53, 0x08 });      // mov rdx, [rbx+8]     — size
        vpShell.AddRange(new byte[] { 0x44, 0x8B, 0x43, 0x10 });      // mov r8d, [rbx+16]    — prot
        vpShell.AddRange(new byte[] { 0x48, 0x8D, 0x4B, 0x14 });      // lea r9, [rbx+20]    — &old (wait, RCX is already set)
        
        // Bug: I overwrote RCX. Let me redo.
        vpShell.Clear();
        vpShell.Add(0x53);                                             // push rbx
        vpShell.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x30 });      // sub rsp, 48
        // RCX = argsAddr
        vpShell.AddRange(new byte[] { 0x48, 0x8B, 0x19 });            // mov rbx, rcx
        vpShell.AddRange(new byte[] { 0x48, 0x8B, 0x0B });            // mov rcx, [rbx]       — addr arg
        vpShell.AddRange(new byte[] { 0x48, 0x8B, 0x53, 0x08 });      // mov rdx, [rbx+8]     — size arg
        vpShell.AddRange(new byte[] { 0x44, 0x8B, 0x43, 0x10 });      // mov r8d, [rbx+0x10]  — prot arg
        vpShell.AddRange(new byte[] { 0x4C, 0x8D, 0x4B, 0x14 });      // lea r9, [rbx+0x14]   — oldOut arg
        vpShell.AddRange(new byte[] { 0x48, 0xB8 });                   // mov rax, vpAddr
        vpShell.AddRange(BitConverter.GetBytes((long)vpAddr));
        vpShell.AddRange(new byte[] { 0xFF, 0xD0 });                   // call rax
        vpShell.AddRange(new byte[] { 0x48, 0x83, 0xC4, 0x30 });      // add rsp, 48
        vpShell.Add(0x5B);                                             // pop rbx
        vpShell.Add(0xC3);                                             // ret

        var vpBytes = vpShell.ToArray();
        var vpCodeAddr = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)vpBytes.Length,
            MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (vpCodeAddr != IntPtr.Zero)
        {
            fixed (byte* p = vpBytes)
                WriteProcessMemory(hProcess, vpCodeAddr, (IntPtr)p, (uint)vpBytes.Length, out _);
            var vpThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, vpCodeAddr, argsAddr, 0, IntPtr.Zero);
            if (vpThread != IntPtr.Zero) { WaitForSingleObject(vpThread, 5000); CloseHandle(vpThread); }
        }

        // Now write the JMP patch (VirtualProtect should have made it writable)
        fixed (byte* p = patch)
            WriteProcessMemory(hProcess, cpwAddr, (IntPtr)p, 14, out _);

        // Restore protection
        BitConverter.GetBytes(PAGE_EXECUTE_READ).CopyTo(args, 12);
        fixed (byte* p = args)
            WriteProcessMemory(hProcess, argsAddr, (IntPtr)p, (uint)args.Length, out _);
        if (vpCodeAddr != IntPtr.Zero)
        {
            var vpThread2 = CreateRemoteThread(hProcess, IntPtr.Zero, 0, vpCodeAddr, argsAddr, 0, IntPtr.Zero);
            if (vpThread2 != IntPtr.Zero) { WaitForSingleObject(vpThread2, 5000); CloseHandle(vpThread2); }
        }

        // Cleanup
        VirtualFreeEx(hProcess, vpCodeAddr, 0, MEM_RELEASE);
        VirtualFreeEx(hProcess, argsAddr, 0, MEM_RELEASE);
    }

    static string EscapeArg(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return "\"\"";
        if (!arg.Contains(' ') && !arg.Contains('"')) return arg;
        return $"\"{arg.Replace("\"", "\\\"")}\"";
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFOW { public uint cb; public IntPtr lpReserved, lpDesktop, lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute;
        public uint dwFlags; public ushort wShowWindow, cbReserved2; public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError; }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public uint dwProcessId, dwThreadId; }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcessW(string? lpApp, string lpCmd, IntPtr pa, IntPtr ta,
        bool inherit, uint flags, IntPtr env, string? dir, ref STARTUPINFOW si, out PROCESS_INFORMATION pi);

    [DllImport("kernel32.dll")] static extern uint ResumeThread(IntPtr h);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h, uint ms);
    [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr h, out uint ec);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll")] static extern IntPtr VirtualAllocEx(IntPtr hp, IntPtr a, uint s, uint t, uint p);
    [DllImport("kernel32.dll")] static extern bool VirtualFreeEx(IntPtr hp, IntPtr a, uint s, uint t);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr hp, IntPtr a, IntPtr b, uint s, out uint w);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr hp, IntPtr a, IntPtr b, uint s, out uint r);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string n);
    [DllImport("kernel32.dll")] static extern IntPtr GetProcAddress(IntPtr m, string n);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr CreateRemoteThread(IntPtr hp, IntPtr ta, uint ss, IntPtr sa, IntPtr p, uint f, IntPtr ti);
}
