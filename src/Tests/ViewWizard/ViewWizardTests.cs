using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.ViewWizard.Tests.BO;

namespace Xpand.XAF.Modules.ViewWizard.Tests{
    public class ViewWizardTests:ViewWizardBaseTest{
        [Test]
        [XpandTest()]
        public void ShowWizard_Action_disabled_always(){
            using (var application = ViewWizardModule().Application){
                var window = application.CreateViewWindow();

                window.SetView(application.NewView<DetailView>(typeof(VW)));

                window.Action<ViewWizardModule>().ShowWizard().Active.ResultValue.ShouldBeFalse();
            }
        }
        [Test]
        [XpandTest()]
        public void ShowWizard_Action_enabled_with_Model(){
            using (var application = ViewWizardModule().Application){
                var modelViewWizard = application.Model.ToReactiveModule<IModelReactiveModulesViewWizard>().ViewWizard;
                var modelViewWizardItem = modelViewWizard.WizardViews.AddNode<IModelWizardView>();
                modelViewWizardItem.DetailView=application.Model.BOModel.GetClass(typeof(VW)).DefaultDetailView;

                var window = application.CreateViewWindow();
                window.SetView(application.NewView<DetailView>(typeof(VW)));

                window.Action<ViewWizardModule>().ShowWizard().Active["Always"].ShouldBeTrue();
            }
        }

    }
}