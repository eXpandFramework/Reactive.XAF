using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Core;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public class ActionRegistrationTests:ReactiveCommonTest{
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [Parallelizable(ParallelScope.Children)]
        public async Task RegisterAction_In_Parallel(int i) {
            var applicationModulesManager = new ApplicationModulesManager(new ControllersManager(), "");
            var id = $"test{i}";
            var simpleAction = await applicationModulesManager.RegisterViewSimpleAction(id).FirstAsync();

            simpleAction.Id.ShouldBe(id);
        }

        [XpandTest()]
        [TestCase(ViewType.DetailView,4)]
        [TestCase(ViewType.ListView,5)]
        public void Register_Simple_Action_For_All_Views(ViewType viewType,int expected){
	        using var application = Platform.Win.NewApplication<ReactiveModule>();
	        var testObserver = application.WhenApplicationModulesManager()
		        .SelectMany(manager => {
			        var registerViewSimpleAction = manager.RegisterViewSimpleAction($"{nameof(SimpleAction)}{viewType}",
				        Configure<SimpleAction>(viewType,nameof(Register_Simple_Action_For_All_Views))).Publish().RefCount();
			        var selectMany = registerViewSimpleAction.SelectMany(action => action.WhenActivated().Select(simpleAction => simpleAction));
			        return registerViewSimpleAction.Merge(selectMany);
		        })
		        .Test();
	        DefaultReactiveModule(application);

	        AssertViewAction<SimpleAction>(application,nameof(Register_Simple_Action_For_All_Views),viewType);
            if (Version.Parse(XafAssemblyInfo.Version) > new Version(20, 2, 0, 0)) {
                testObserver.ItemCount.ShouldBe(expected);
            }
            
        }

        private Action<T> Configure<T>(ViewType viewType,string caption) where T:ActionBase 
            => action => {
                action.TargetViewType = viewType;
                action.Caption = caption;
                action.TargetObjectType = typeof(NonPersistentObject);
                if (action is SingleChoiceAction singleChoiceAction){
                    singleChoiceAction.Items.Add(new ChoiceActionItem("test", null));
                }
            };

        [XpandTest()][Test]
        public void Register_SimpleAction_For_Windows(){
	        using var application = Platform.Win.NewApplication<ReactiveModule>();
	        application.WhenApplicationModulesManager()
		        .SelectMany(manager => manager.RegisterWindowSimpleAction(nameof(SimpleAction)))
		        .TakeUntilDisposed(application)
		        .Subscribe();
	        DefaultReactiveModule(application);

	        AssertWindowAction<SimpleAction>(application);
        }

        private static T AssertWindowAction<T>(XafApplication application) where T:ActionBase{
            var window = application.CreateViewWindow();
            var actionBase = window.Controllers.Values.OfType<WindowController>().SelectMany(controller => controller.Actions)
                .FirstOrDefault(a => a.Id == typeof(T).Name);
            actionBase.ShouldNotBeNull();
            actionBase.Active.ResultValue.ShouldBe(true);
            return (T) actionBase;
        }

        private T AssertViewAction<T>(XafApplication application,string caption,ViewType viewType=ViewType.DetailView) where T:ActionBase{
            var viewWindow = application.CreateViewWindow();
            var compositeView = application.NewView(application.FindDetailViewId(typeof(NonPersistentObject)));
            viewWindow.SetView(compositeView);
            var id = $"{typeof(T).Name}{viewType}";
            var modelAction = compositeView.Model.Application.ActionDesign.Actions.FirstOrDefault(action => action.Id==id);
            modelAction.ShouldNotBeNull();
            modelAction.TargetViewType.ShouldBe(viewType);
            modelAction.TargetObjectType.ShouldBe(typeof(NonPersistentObject).FullName);
            modelAction.Caption.ShouldBe(caption);
            var detailViewAction = viewWindow.Actions(id).FirstOrDefault();
            detailViewAction.ShouldNotBeNull();
            detailViewAction.Active.ResultValue.ShouldBe(viewType==ViewType.DetailView);
            var listViewAction = viewWindow.View.AsDetailView().GetListPropertyEditor<NonPersistentObject>(o => o.Childs).Frame.Actions(id).FirstOrDefault();
            if (viewType==ViewType.ListView) {
                listViewAction.ShouldNotBeNull();
                listViewAction.Active.ResultValue.ShouldBe(true);
            }
            var window = application.CreateViewWindow();
            window.Controllers.Values.OfType<WindowController>().SelectMany(controller => controller.Actions).Select(a => a.Id).ShouldNotContain(id);
            if (detailViewAction.Active.ResultValue){
                var testObserver = detailViewAction.WhenExecuted().ToUnit().Test();
                if (detailViewAction is PopupWindowShowAction popupWindowShowAction) {
                    popupWindowShowAction.IsModal = false;
                    testObserver=popupWindowShowAction.WhenCustomizePopupWindowParams().Do(args => args.View = application.NewView<DetailView>(typeof(R))).FirstAsync().ToUnit().Test();
                }
                
                detailViewAction.DoTheExecute();
                testObserver.ItemCount.ShouldBe(1);
            }
            else if (listViewAction != null && listViewAction.Active.ResultValue){
                var testObserver = listViewAction.WhenExecuted().Test();
                listViewAction.DoTheExecute();
                testObserver.ItemCount.ShouldBe(1);
            }
            detailViewAction.TargetViewType.ShouldBe(viewType);
            detailViewAction.TargetObjectType.ShouldBe(typeof(NonPersistentObject));
            detailViewAction.Caption.ShouldBe(caption);
            if (listViewAction != null) {
                listViewAction.TargetViewType.ShouldBe(viewType);
                listViewAction.TargetObjectType.ShouldBe(typeof(NonPersistentObject));
                listViewAction.Caption.ShouldBe(caption);
            }

            return (T)detailViewAction;
        }

        [XpandTest()]
        [TestCase(ViewType.DetailView)]
        [TestCase(ViewType.ListView)]
        public void Register_Single_Choice_Action_For_Views(ViewType viewType){
	        using var application = Platform.Win.NewApplication<ReactiveModule>();
	        application.WhenApplicationModulesManager()
		        .SelectMany(manager => manager.RegisterViewSingleChoiceAction($"{nameof(SingleChoiceAction)}{viewType}",
			        Configure<SingleChoiceAction>(viewType, nameof(Register_Single_Choice_Action_For_Views))))
		        .TakeUntilDisposed(application)
		        .Subscribe();
	        DefaultReactiveModule(application);

	        var singleChoiceAction = AssertViewAction<SingleChoiceAction>(application,nameof(Register_Single_Choice_Action_For_Views),viewType);
	        singleChoiceAction.Items.Select(item => item.Id).ShouldContain("test");
        }
        [XpandTest()][Test]
        public void Register_SingleChoice_Action_For_Windows(){
	        using var application = Platform.Win.NewApplication<ReactiveModule>();
	        application.WhenApplicationModulesManager()
		        .SelectMany(manager => manager.RegisterWindowSingleChoiceAction(nameof(SingleChoiceAction),
			        configure:Configure<SingleChoiceAction>(ViewType.Any, nameof(Register_SingleChoice_Action_For_Windows))))
		        .TakeUntilDisposed(application)
		        .Subscribe();
	        DefaultReactiveModule(application);

	        var singleChoiceAction = AssertWindowAction<SingleChoiceAction>(application);
	        singleChoiceAction.Items.Select(item => item.Id).ShouldContain("test");
        }
        [XpandTest()][TestCase(ViewType.DetailView)]
        [TestCase(ViewType.ListView)]
        public void Register_Parameterized_Action_For_Views(ViewType viewType){
	        using var application = Platform.Win.NewApplication<ReactiveModule>();
	        application.WhenApplicationModulesManager()
		        .SelectMany(manager => manager.RegisterViewParametrizedAction($"{nameof(ParametrizedAction)}{viewType}",
			        typeof(int),configure: Configure<ParametrizedAction>(viewType, nameof(Register_Parameterized_Action_For_Views))))
		        .TakeUntilDisposed(application)
		        .Subscribe();
	        DefaultReactiveModule(application);

	        AssertViewAction<ParametrizedAction>(application,nameof(Register_Parameterized_Action_For_Views),viewType);
        }
        [XpandTest()][Test]
        public void Register_Parameterized_Action_For_Windows(){
	        using var application = Platform.Win.NewApplication<ReactiveModule>();
	        application.WhenApplicationModulesManager()
		        .SelectMany(manager => manager.RegisterWindowParametrizedAction(nameof(ParametrizedAction),typeof(int)))
		        .TakeUntilDisposed(application)
		        .Subscribe();
	        DefaultReactiveModule(application);

	        AssertWindowAction<ParametrizedAction>(application);
        }
        [XpandTest()][TestCase(ViewType.DetailView)]
        public void Register_Popup_Action_For_Views(ViewType viewType){
	        using var application = Platform.Win.NewApplication<ReactiveModule>();
	        application.WhenApplicationModulesManager()
		        .SelectMany(manager => manager.RegisterViewPopupWindowShowAction($"{nameof(PopupWindowShowAction)}{viewType}",
			        Configure<PopupWindowShowAction>(viewType, nameof(Register_Popup_Action_For_Views))))
		        .TakeUntilDisposed(application)
		        .Subscribe();
	        DefaultReactiveModule(application);

	        AssertViewAction<PopupWindowShowAction>(application,nameof(Register_Popup_Action_For_Views),viewType);
        }
        [XpandTest()][Test]
        public void Register_Popup_Action_For_Windows(){
	        using var application = Platform.Win.NewApplication<ReactiveModule>();
	        application.WhenApplicationModulesManager()
		        .SelectMany(manager => manager.RegisterWindowPopupWindowShowAction(nameof(PopupWindowShowAction)))
		        .TakeUntilDisposed(application)
		        .Subscribe();
	        DefaultReactiveModule(application);
            
	        AssertWindowAction<PopupWindowShowAction>(application);
        }

    }
}