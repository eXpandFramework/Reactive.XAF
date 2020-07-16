using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.XAF.Modules.ViewWizard.Tests.BO;

namespace Xpand.XAF.Modules.ViewWizard.Tests{
    public abstract class ViewWizardBaseTest:BaseTest{
        protected static ViewWizardModule ViewWizardModule(params ModuleBase[] modules){
            var positionInListViewModule = Platform.Win.NewApplication<ViewWizardModule>().AddModule<ViewWizardModule>(typeof(VW));
            var xafApplication = positionInListViewModule.Application;
            xafApplication.Modules.AddRange(modules);
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return positionInListViewModule;
        }
    }
}