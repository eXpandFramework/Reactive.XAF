using System.IO;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static void DeleteUserModelDifferences(this XafApplication application) {
            if (application.GetPlatform() != Platform.Win) return;
            Directory.GetFiles((string)application.GetPropertyValue("UserModelDifferenceFilePath"), "Model.User.xafml")
                .Execute(File.Delete).Enumerate();
        }
    }
}