using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.BO;

namespace Xpand.XAF.Modules.LookupDefaultObject.Tests{
    public abstract class LookupDefaultObjectBaseTest:BaseTest{
        protected static LookupDefaultObjectModule LookupDefaultObjectModule(params ModuleBase[] modules){
            var positionInListViewModule = Platform.Win.NewApplication<LookupDefaultObjectModule>().AddModule<LookupDefaultObjectModule>(typeof(Order),typeof(Accessory),typeof(Product));
            var xafApplication = positionInListViewModule.Application;
            xafApplication.Modules.AddRange(modules);
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return positionInListViewModule;
        }

    }
}