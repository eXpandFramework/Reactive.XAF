namespace Xpand.Extensions.Windows.Manager{
    internal static class UnsafeNativeMethods{
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern MsgBoxResult MessageBox(System.IntPtr hWnd, string text, string caption,
            MsgBoxStyle options);
    }
}