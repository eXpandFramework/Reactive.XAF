using System;
using System.IO;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.AppDomainExtensions {
    public static partial class AppDomainExtensions {
        public static void DeleteUserModelDifferences(this AppDomain appDomain)
            => Directory.GetFiles(appDomain.ApplicationPath(), "Model.User.xafml")
                .Execute(File.Delete).Enumerate();
    }
}