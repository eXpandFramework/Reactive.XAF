using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.SystemModule;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Windows.Tests.BOModel;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")] 

namespace Xpand.XAF.Modules.Windows.Tests{
    [NonParallelizable][SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public class ExitTests : BaseWindowsTest{
    
        
        [Test()]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public void Hide_On_Exit(){
            using var application = WindowsModule().Application;
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.Exit.OnExit.HideMainWindow = true;
            var test = application.WhenWindowCreated(true)
                .Cast<Window>()
                .Select(frame => {
                    frame.Close();
                    var form = (Form) frame.Template;
                    return form.Visible;
                })
                .Do(_ => application.Exit())
                .Test();


            ((TestWinApplication)application).Start();

            test.AssertValues(false);
            
        }

        
        [XpandTest][Test][Combinatorial]
        [Apartment(ApartmentState.STA)]
        public void CloseWindow_On([Values(false, true)] bool hideMainWindow, [Values(false, true)] bool popup,
            [Values("OnDeactivate", "OnKeyDown")] string when, [Values(nameof(IModelOn.ExitApplication),nameof(IModelOn.CloseWindow))] string action) {
            if (hideMainWindow && !popup && when == "OnDeactivate") {
                return;
            }
            using var application = WindowsModule().Application;
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.Exit.OnEscape.SetPropertyValue(action, when == "OnKeyDown");
            modelWindows.Exit.OnDeactivation.SetPropertyValue(action, when == "OnDeactivate");
            modelWindows.Exit.OnExit.HideMainWindow = hideMainWindow;
            modelWindows.Exit.OnDeactivation.ApplyInMainWindow = popup;
            application.WhenWindowCreated(true)
                .WhenNotDefault(_ => popup)
                .Do(_ => {
                    var window = application.CreateWindow(TemplateContext.PopupWindow, new List<Controller>(), false, false);
                    window.SetView(application.NewDetailView(typeof(W)));
                    
                }).Test();
            var test = application.WhenWindowCreated(!popup)
                .SelectMany(window => popup?window.WhenTemplateViewChanged():window.ReturnObservable())
                .When(!popup?TemplateContext.ApplicationWindow:TemplateContext.PopupWindow)
                .Select(frame => {
                    var eventArgs =when=="OnKeyDown"? new KeyEventArgs(Keys.Escape):new EventArgs();
                    frame.Template.CallMethod("OnActivated",eventArgs);
                    frame.Template.CallMethod(when,eventArgs);
                    if (hideMainWindow && !popup) return frame.Template==null;
                    return frame.Template != null;
                })
                .Do(_ => {
                    if (hideMainWindow||popup) {
                        application.Exit();
                    }
                })
                .Test();

            
            ((TestWinApplication)application).Start();

            if (!hideMainWindow && action != nameof(IModelOn.ExitApplication)) {
                test.AssertValues(action == nameof(IModelOn.ExitApplication));
            }
            
            
        }



        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public  void Minimize_On_Exit(){
	        using var application = WindowsModule().Application;
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.Exit.OnExit.MinimizeMainWindow = true;
            var test = application.WhenWindowCreated(true)
                .Select(window => {
                    window.Close();
                    var form = (Form) window.Template;
                    return form.WindowState;
                })
                .Do(_ => application.Exit())

	            .Test();

            ((TestWinApplication)application).Start();

            test.AssertValues(FormWindowState.Minimized);

        }


        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public  void Edit_Model(){
            using var application = WindowsModule().Application;
            application.PatchFormShowDialog();

            application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows.EnableExit();

            var mainWindow = application.WhenWindowCreated(true).Publish().RefCount();
            var test = mainWindow.FirstAsync()
                .Select(window => {
                    window.GetController<EditModelController>().EditModelAction.DoExecute();
                    var form = (Form) window.Template;
                    return form;
                })
                .Do(_ => application.Exit())
                .WhenNotDefault()
                .Test();

            ((TestWinApplication)application).Start();

            test.AssertCompleted();
            test.AssertValueCount(0);


        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public void LogOffApplication() {
            using var application = Platform.Win.NewApplication<WindowsModule>();
            application.SetupSecurity();
            application.AddModule<WindowsModule>();

            application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows.EnableExit();

            application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            
            application.Logon();
            application.PatchFormShowDialog();
            application.LogOff();

            
        }

        [TestCase(DialogResult.No)]
        [TestCase(DialogResult.Yes)]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public  void PromptOn_Exit(DialogResult dialogResult){
            using var application = WindowsModule().Application;
            
            WinApplication.Messaging.MockMessaging(() => dialogResult);

            var testObserver = WinApplication.Messaging.WhenConfirmationDialogClosed()
                .Select(args => args.DialogResult).Test();
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.Exit.Prompt.Enabled = true;
            var test = application.WhenWindowCreated(true)
                .Select(window => {
                    window.Close();
                    var form = (Form) window.Template;
                    return form?.Visible ?? dialogResult == DialogResult.Yes;
                })
                .Do(_ => application.Exit())
                .Test();

            ((TestWinApplication)application).Start();

            test.AssertValues(true);
            testObserver.Items.First().ShouldBe(dialogResult);
            
        }

    }

}