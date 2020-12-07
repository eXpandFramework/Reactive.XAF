using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.BO;

namespace Xpand.XAF.Modules.ViewItemValue.Tests{
    public abstract class ViewItemValueCommonTest:BaseTest{
        protected static ViewItemValueModule ViewItemValueModule(params ModuleBase[] modules){
            var positionInListViewModule = Platform.Win.NewApplication<ViewItemValueModule>().AddModule<ViewItemValueModule>(typeof(Order),typeof(Accessory),typeof(Product));
            var xafApplication = positionInListViewModule.Application;
            xafApplication.Modules.AddRange(modules);
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return positionInListViewModule;
        }

    }
}