using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.OneView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Win.Services;
using Xunit;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.OneView.Tests{
    [Collection(nameof(OneView.OneViewModule))]
    public class OneViewTests : BaseTest{
        [WinFormsFact]
        public void Hide_MainWindow_Template_OnStart(){
            using (var application = OneViewModule().Application){

                var visibility = application.WhenViewOnFrame(typeof(OV))
                    .SelectMany(frame => frame.Template.ToForm().WhenShown())
                    .Select(form => application.MainWindow.Template.ToForm().Visible)
                    .FirstAsync()
                    .Do(form => application.Exit())
                    .SubscribeReplay();

                ((TestWinApplication)application).Start();

                visibility.Test().AssertValues(false);

            }
        }

        [WinFormsFact]
        public async Task Show_OneView_OnStart(){
            using (var application = (TestWinApplication) OneViewModule().Application){

                var replay = WhenOVShown(application)
                    .Do(frame => application.Exit())
                    .FirstAsync()
                    .SubscribeReplay();

                application.Start();

                var frame1 = await replay;
                frame1.ShouldNotBeNull();
            }
        }

        private static IObservable<View> WhenOVShown(XafApplication application){
            return application.WhenViewShown()
                .Select(_ => _.TargetFrame).WhenView(typeof(OV))
                .SelectMany(frame => ((Form) frame.Template)
                    .WhenActivated().FirstAsync().To(frame.View));
        }

        [WinFormsFact]
        public async Task Edit_Model(){
            using (var application = OneViewModule().Application){
                application.WhenViewOnFrame(typeof(OV))
                    .SelectMany(frame => frame.Template.ToForm().WhenShown().To(frame))
                    .Do(frame => frame.GetController<DialogController>().AcceptAction.DoExecute())
                    .SubscribeReplay();
                var testWinApplication = ((TestWinApplication) application);
                var editModel = testWinApplication.ModelEditorForm.SelectMany(form => form.WhenLoad())
                    .Do(form => application.Exit())
                    .FirstAsync()
                    .SubscribeReplay();
            
                
                testWinApplication.Start();
                await editModel;
            }
        }

        [WinFormsFact]
        public async Task Exit_Application_On_View_Close(){
            using (var application = OneViewModule().Application){
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


        private static OneViewModule OneViewModule(Platform platform=Platform.Win){
            var application = platform.NewApplication();
            
            var oneViewModule = application.AddModule<OneViewModule>(typeof(OV), typeof(A));
            ConfigureModel(application);
            return oneViewModule;
        }

        private static void ConfigureModel(XafApplication application){
            ((IModelApplicationNavigationItems)application.Model).NavigationItems.StartupNavigationItem.View =
                application.FindModelListView(typeof(TraceEvent));
            var oneView = application.Model.ToReactiveModule<IModelReactiveModuleOneView>().OneView;
            oneView.View = application.FindModelListView(typeof(OV));
        }
    }
}