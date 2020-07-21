using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
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

        [Test]
        [XpandTest()]
        public void ShowWizard_Action_Items(){
            using (var application = ViewWizardModule().Application){
                var wizardWizardViews = application.Model.ToReactiveModule<IModelReactiveModulesViewWizard>().ViewWizard.WizardViews;
                var modelWizardView = wizardWizardViews.AddNode<IModelWizardView>();
                modelWizardView.DetailView = application.Model.BOModel.GetClass(typeof(VW)).DefaultDetailView;
                var window = application.CreateViewWindow();

                window.SetView(application.NewView<DetailView>(typeof(VW)));

                var showWizard = window.Action<ViewWizardModule>().ShowWizard();
                showWizard.Items.Count.ShouldBe(1);
                var item = showWizard.Items.First();
                item.Data.ShouldBe(modelWizardView);
                item.Caption.ShouldBe(modelWizardView.DetailView.Caption);
            }
        }
        [Test]
        [XpandTest()]
        public void ShowWizard_Action_Shows_Wizard_DetailView(){
            using (var application = ViewWizardModule().Application){
                var wizardWizardViews = application.Model.ToReactiveModule<IModelReactiveModulesViewWizard>().ViewWizard.WizardViews;
                var modelWizardView = wizardWizardViews.AddNode<IModelWizardView>();
                modelWizardView.DetailView = application.Model.BOModel.GetClass(typeof(VW)).DefaultDetailView;
                var window = application.CreateViewWindow();
                window.SetView(application.NewView<DetailView>(typeof(VW)));
                var showWizard = window.Action<ViewWizardModule>().ShowWizard();

                showWizard.WhenExecuted().Do(e => e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).Test();
                var whendetailViewCreated = application.WhenDetailViewCreated().ToDetailView().Test();

                showWizard.DoExecute(showWizard.Items.First());

                whendetailViewCreated.Items.Count.ShouldBe(1);
                whendetailViewCreated.Items.First().Model.ShouldBe(modelWizardView.DetailView);

                
            }
        }
        [Test]
        [XpandTest()]
        public void NextWizard_Action_Shows_NextWizard_DetailView(){
            using (var application = ViewWizardModule().Application){
                var wizardWizardViews = application.Model.ToReactiveModule<IModelReactiveModulesViewWizard>().ViewWizard.WizardViews;
                var modelWizardView = wizardWizardViews.AddNode<IModelWizardView>();
                modelWizardView.DetailView = application.Model.BOModel.GetClass(typeof(VW)).DefaultDetailView;
                var childItem = modelWizardView.Childs.AddNode<IModelViewWizardChildItem>();
                childItem.ChildDetailView = (IModelDetailView) application.Model.Views["VW_Child_DetailView1"];
                childItem = modelWizardView.Childs.AddNode<IModelViewWizardChildItem>();
                childItem.ChildDetailView = (IModelDetailView) application.Model.Views["VW_Child_DetailView2"];
                var window = application.CreateViewWindow();
                window.SetView(application.NewView<DetailView>(typeof(VW)));
                var showWizard = window.Action<ViewWizardModule>().ShowWizard();
                showWizard.WhenExecuted().Do(e => e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).Test();
                var nextFrame = application.WhenViewOnFrame().Test();
                showWizard.DoExecute(showWizard.Items.First());
                var nextDetailView = application.WhenDetailViewCreated().Test();
                var nextWizardView = nextFrame.Items.First().Action<ViewWizardModule>().NextWizardView();

                nextWizardView.DoExecute();

                nextDetailView.Items.Count.ShouldBe(1);
                
                nextDetailView = application.WhenDetailViewCreated().Test();
                nextWizardView.DoExecute();

                nextDetailView.Items.Count.ShouldBe(1);
            }
        }
        [Test]
        [XpandTest()]
        public void PreviousWizard_Action_Shows_PreviousWizard_DetailView(){
            using (var application = ViewWizardModule().Application){
                var wizardWizardViews = application.Model.ToReactiveModule<IModelReactiveModulesViewWizard>().ViewWizard.WizardViews;
                var modelWizardView = wizardWizardViews.AddNode<IModelWizardView>();
                modelWizardView.DetailView = application.Model.BOModel.GetClass(typeof(VW)).DefaultDetailView;
                var childItem = modelWizardView.Childs.AddNode<IModelViewWizardChildItem>();
                childItem.ChildDetailView = (IModelDetailView) application.Model.Views["VW_Child_DetailView1"];
                childItem = modelWizardView.Childs.AddNode<IModelViewWizardChildItem>();
                childItem.ChildDetailView = (IModelDetailView) application.Model.Views["VW_Child_DetailView2"];
                var window = application.CreateViewWindow();
                window.SetView(application.NewView<DetailView>(typeof(VW)));
                var showWizard = window.Action<ViewWizardModule>().ShowWizard();
                showWizard.WhenExecuted().Do(e => e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).Test();
                var nextFrame = application.WhenViewOnFrame().Test();
                showWizard.DoExecute(showWizard.Items.First());
                var previousWizardView = nextFrame.Items.First().Action<ViewWizardModule>().PreviousWizardView();
                previousWizardView.Enabled.ResultValue.ShouldBeFalse();
                var nextWizardView = nextFrame.Items.First().Action<ViewWizardModule>().NextWizardView();
                nextWizardView.DoExecute();
                previousWizardView.Enabled.ResultValue.ShouldBeTrue();
                var nextDetailView = application.WhenDetailViewCreated().Test();

                previousWizardView.DoExecute();

                nextDetailView.Items.Count.ShouldBe(1);
                
            }
        }

    }
}