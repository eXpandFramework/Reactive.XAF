using System;
using System.IO;

namespace Xpand.Extensions.AppDomainExtensions {
    public static partial class AppDomainExtensions {
        public static void DeleteFolder(this AppDomain appDomain, string name, bool recursive = true) {
            var path = $"{appDomain.ApplicationPath()}{name}";
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive);
            }
        }
    }
}