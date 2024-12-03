﻿using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;
using Xpand.XAF.Modules.Windows.SystemActions;

namespace Xpand.XAF.Modules.Windows{
    public static class WindowsService{
        
        internal static IObservable<Unit> WindowsConnect(this  ApplicationModulesManager manager) 
	        => manager.WhenApplication(application => application.WhenWindowTemplate()
                .MultiInstance().Startup().ConfigureForm()
                .NotifyIcon()
                .ExitAfterModelEdit().CancelExit()
                .OnWindowDeactivation(window => !window.Model().Exit.OnDeactivation.ApplyInMainWindow)
                .OnWindowEscape()
                .Merge(application.WhenPopupWindows()
                    .ConfigureForm(window => window.Model().Form.PopupWindows)
                    .OnWindowDeactivation().OnWindowEscape()).ToUnit()
                .Merge(application.SystemActionsConnect())
            ).ToUnit();

        private static IObservable<Window> WhenPopupWindows(this XafApplication application) 
            => application.WhenPopupWindowCreated().TemplateChanged();

        private static IObservable<Window> Startup(this IObservable<Window> source)
            => source.Do(frame => {
                string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var path = deskDir + "\\" + frame.Application.Title + ".url";
                if (frame.Model().Startup) {
                    using StreamWriter writer = new StreamWriter(path);
                    string app = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    writer.WriteLine("[InternetShortcut]");
                    writer.WriteLine("URL=file:///" + app);
                    writer.WriteLine("IconIndex=0");
                    string icon = app.Replace('\\', '/');
                    writer.WriteLine("IconFile=" + icon);
                }
                else if (File.Exists(path)) {
                    File.Delete(path);
                }
            });

        private static IObservable<Window> ConfigureForm(this IObservable<Window> source,Func<Window,bool> apply=null)
	        => source.SelectMany(frame => apply == null || apply(frame) ? frame.ConfigureForm() : frame.Observe());

        private static IObservable<Window> ConfigureForm(this Window frame) {
            var form = ((Form)frame.Template);
            var modelForm = frame.Model().Form;
            form.MinimizeBox = modelForm.MinimizeBox;
            form.MaximizeBox = modelForm.MaximizeBox;
            form.ControlBox = modelForm.ControlBox;
            form.ShowInTaskbar = modelForm.ShowInTaskbar;
            form.FormBorderStyle = modelForm.FormBorderStyle;
            return modelForm.Text != null ? form.WhenEvent(nameof(Form.TextChanged))
                .Where(_ => form.Text != modelForm.Text)
                .Do(_ => form.Text = modelForm.Text).To(frame) : frame.Observe()
                .TraceWindows();
        }

        internal static IModelWindows Model(this XafApplication application) 
            => application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
        
        internal static IModelWindows Model(this Frame frame) 
            => frame.Application.Model();

        public static IObservable<Unit> AddMessages(this IObservable<WindowTemplateController> source,Func<IObservable<string>> messagesSource,int seconds=1) 
            => source.SelectMany(controller => {
                    var observeLatestOnContext = messagesSource().ObserveLatestOnContext().Replay(1);
                    var connect = observeLatestOnContext.Connect();
                    return seconds.Seconds().Interval().ObserveLatestOnContext()
                        .ExhaustMap(_ => controller.WhenAddMessage())
                        .SelectMany(e => observeLatestOnContext.Take(1).Do(s => e.StatusMessages.Add(s)))
                        .TakeUntil(controller.WhenDisposed().Do(_ => connect.Dispose()));
                })
                    
                .ToUnit();

        private static IObservable<CustomizeWindowStatusMessagesEventArgs> WhenAddMessage(this WindowTemplateController controller) 
            => controller.WhenCustomizeWindowStatusMessages().Take(1)
                .Merge(controller.DeferAction(controller.UpdateWindowStatusMessage).To<CustomizeWindowStatusMessagesEventArgs>())
                .Take(1);

        internal static IObservable<TSource> TraceWindows<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
	        => source.Trace(name, WindowsModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
        
    }

}