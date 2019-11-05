using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.XAF.Modules.OneView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Win.Services;


namespace Xpand.XAF.Modules.OneView.Tests{
    [NonParallelizable]
    public class OneViewTests : BaseTest{
        [Test]
        [Apartment(ApartmentState.STA)]
        public  void Hide_MainWindow_Template_OnStart(){
            using (var application = OneViewModule(nameof(Hide_MainWindow_Template_OnStart)).Application){

                var visibility = application.WhenViewOnFrame(typeof(OV))
                    .SelectMany(frame => frame.Template.ToForm().WhenShown())
                    .Select(form => application.MainWindow.Template.ToForm().Visible)
                    .FirstAsync()
                    .Do(form => {

                        application.Exit();
                    })
                    .SubscribeReplay();

                ((TestWinApplication)application).Start();

                visibility.Test().AssertValues(false);
                
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Show_OneView_OnStart(){
            using (var application = (TestWinApplication) OneViewModule(nameof(Show_OneView_OnStart)).Application){

                var replay = application.WhenViewShown()
                    .Select(_ => _.TargetFrame).WhenView(typeof(OV))
                    .SelectMany(frame => ((Form) frame.Template)
                        .WhenActivated().FirstAsync().To(frame.View))
                    .Do(frame => application.Exit())
                    .FirstAsync()
                    .SubscribeReplay();

                application.Start();

                var frame1 = await replay;
                frame1.ShouldNotBeNull();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Exit_Application_On_View_Close(){
            using (var application = OneViewModule(nameof(Exit_Application_On_View_Close)).Application){
                var closeView = application.WhenViewOnFrame(typeof(OV))
                    .SelectMany(frame => frame.Template.ToForm().WhenShown().To(frame))
                    .CombineLatest(application.WhenWindowCreated(true), (frame, window) => frame)
                    .Do(frame => frame.View.Close())
                    .FirstAsync()
                    .SubscribeReplay();

                var testWinApplication = ((TestWinApplication) application);
                
                testWinApplication.Start();

                await closeView;
            }
        }


        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Edit_Model(){
            using (var application = OneViewModule(nameof(Edit_Model)).Application){
                var whenViewOneFrame=application.WhenViewOnFrame(typeof(OV))
                    .SelectMany(frame => frame.Template.ToForm().WhenShown().To(frame))
                    .Do(frame => frame.GetController<DialogController>().AcceptAction.DoExecute())
                    .FirstAsync()
                    .SubscribeReplay();
                var testWinApplication = ((TestWinApplication) application);
                var editModel = testWinApplication.ModelEditorForm.SelectMany(form => form.WhenShown())
                    .SelectMany(form => {
                        form.Close();
                        return form.WhenDisposed();
                    })
                    .Select(tuple => tuple)
                    .FirstAsync()
                    .SubscribeReplay();
                var showViewAfterModelEdit = application.WhenViewOnFrame(typeof(OV)).Select(frame => frame).When(TemplateContext.PopupWindow)
                    .SkipUntil(editModel)
                    .SelectMany(frame => frame.Template.ToForm().WhenShown().To(frame))
                    .Do(frame => frame.View.Close())
                    .FirstAsync().SubscribeReplay();

                testWinApplication.Start();
                await editModel;
                await showViewAfterModelEdit;
                await whenViewOneFrame;
            }
        }

        private static OneViewModule OneViewModule(string title,Platform platform=Platform.Win){
            var application = platform.NewApplication<OneViewModule>();
            application.EditorFactory=new EditorsFactory();
            var oneViewModule = application.AddModule<OneViewModule>(title,typeof(OV), typeof(A));
            ConfigureModel(application);
            return oneViewModule;
        }

        private static void ConfigureModel(XafApplication application){
            var startupNavigationItem = ((IModelApplicationNavigationItems)application.Model).NavigationItems.StartupNavigationItem;
            if (startupNavigationItem != null){
                startupNavigationItem.View = application.FindModelListView(typeof(TraceEvent));
            }
            var oneView = application.Model.ToReactiveModule<IModelReactiveModuleOneView>().OneView;
            oneView.View = application.FindModelListView(typeof(OV));
        }
    }
}