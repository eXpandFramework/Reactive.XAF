using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.ViewWizard.Tests.BO;

namespace Xpand.XAF.Modules.ViewWizard.Tests{
    public abstract class ViewWizardBaseTest:BaseTest{
        protected static ViewWizardModule ViewWizardModule(params ModuleBase[] modules){
            var application = Platform.Win.NewApplication<ViewWizardModule>();
            application.Modules.Add(new CloneModelViewModule());
            var positionInListViewModule = application.AddModule<ViewWizardModule>(typeof(VW));
            var xafApplication = positionInListViewModule.Application;
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return positionInListViewModule;
        }
    }
}