using System.Diagnostics;

namespace Xpand.Extensions.ProcessExtensions {
    public static partial class ProcessExtensions {
        public static Process Start(this ProcessStartInfo processStartInfo)
            => Process.Start(processStartInfo);
    }
}