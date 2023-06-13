using System;
using System.Diagnostics.CodeAnalysis;
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
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.OneView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Services;

using A = Xpand.XAF.Modules.OneView.Tests.BOModel.A;


namespace Xpand.XAF.Modules.OneView.Tests{
    [NonParallelizable][SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public class OneViewTests : BaseTest{
        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public  void Hide_MainWindow_Template_OnStart(){
	        using var application = OneViewModule().Application;
	        var visibility = application.WhenViewOnFrame(typeof(OV))
		        .SelectMany(frame => frame.Template.WhenWindowsForm().When("Shown"))
		        .Select(_ => ((Form) application.MainWindow.Template).Visible)
		        .FirstAsync()
		        .Do(_ => application.Exit())
		        .SubscribeReplay();

	        ((TestWinApplication)application).Start();

	        visibility.Test().AssertValues(false);
        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public void Show_OneView_OnStart(){
	        using var application = (TestWinApplication) OneViewModule().Application;
	        var test = application.WhenViewOnFrame(typeof(OV))
		        .Select(frame => frame)
		        .Do(frame => frame.Application.Exit())
		        .Test();

	        application.Start();

	        test.ItemCount.ShouldBe(1);
        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Exit_Application_On_View_Close(){
	        using var application = OneViewModule().Application;
	        var closeView = application.WhenViewOnFrame(typeof(OV))
		        .SelectMany(frame => frame.Template.WhenWindowsForm().When("Shown").To(frame).Select(frame1 => frame1))
		        .Do(frame => frame.View.Close())
		        .FirstAsync()
		        .SubscribeReplay();

	        var testWinApplication = ((TestWinApplication) application);
                
	        testWinApplication.Start();

	        await closeView;
        }


        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public void Edit_Model(){
	        using var application = OneViewModule().Application;
	        var whenViewOneFrame=application.WhenViewOnFrame(typeof(OV))
		        .SelectMany(frame => frame.Template.WhenWindowsForm().When("Shown").To(frame))
		        .Do(frame => frame.GetController<DialogController>().AcceptAction.DoExecute())
		        .FirstAsync()
		        .SubscribeReplay();
	        var testWinApplication = ((TestWinApplication) application);
	        var editModel = testWinApplication.ModelEditorForm.SelectMany(form => form.WhenEvent(nameof(Form.Shown)).To(form))
		        .SelectMany(form => {
			        form.Close();
			        return form.WhenDisposed();
		        })
		        .Select(tuple => tuple)
		        .FirstAsync()
		        .SubscribeReplay();
	        var showViewAfterModelEdit = application.WhenViewOnFrame(typeof(OV)).Select(frame => frame).When(TemplateContext.PopupWindow)
		        .SkipUntil(editModel)
		        .SelectMany(frame => frame.Template.WhenWindowsForm().When("Shown").To(frame))
		        .Do(frame => frame.View.Close())
		        .FirstAsync().SubscribeReplay();
	        testWinApplication.WhenWin().WhenCustomHandleException().Subscribe(args => { args.handledEventArgs.Handled = true; });
	        testWinApplication.Start();
	        Await(async () => {
                await editModel;
                await showViewAfterModelEdit;
                await whenViewOneFrame;
            });
        }

        private static OneViewModule OneViewModule(Platform platform=Platform.Win){
            var application = platform.NewApplication<OneViewModule>(handleExceptions:false);
            application.EditorFactory=new EditorsFactory();
            var oneViewModule = application.AddModule<OneViewModule>(typeof(OV), typeof(A));
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