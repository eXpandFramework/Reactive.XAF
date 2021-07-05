using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Windows{
    internal static class WindowsService{
        
        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager) 
	        => manager.WhenApplication(application => application.WhenWindowTemplate().Cast<Window>()
                .MultiInstance()
                .Startup().ConfigureForm().NotifyIcon()
                .CancelExit()
                .OnWindowDeactivation().OnWindowEscape()
                .Merge(application.WhenPopupWindows()
                    .ConfigureForm(window => window.Application.Model().Form.PopupWindows)
                    .OnWindowDeactivation().OnWindowEscape())
            ).ToUnit();

        private static IObservable<Window> WhenPopupWindows(this XafApplication application) 
            => application.WhenPopupWindowCreated().TemplateChanged();

        private static IObservable<Window> Startup(this IObservable<Window> source)
            => source.Do(frame => {
                string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var path = deskDir + "\\" + frame.Application.Title + ".url";
                if (frame.Application.Model().Startup) {
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
	        => source.Do(frame => {
                if (apply == null || apply(frame)) {
                    var form = ((Form) frame.Template);
                    var controlBox = frame.Application.Model().Form;
                    form.MinimizeBox = controlBox.MinimizeBox;
                    form.MaximizeBox = controlBox.MaximizeBox;
                    form.ControlBox = controlBox.ControlBox;
                    form.ShowInTaskbar = controlBox.ShowInTaskbar;
                    form.FormBorderStyle  = controlBox.FormBorderStyle ;
                }
            });

        internal static IModelWindows Model(this XafApplication application) 
            => application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;

        internal static IObservable<TSource> TraceWindows<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
	        => source.Trace(name, WindowsModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName);
    }

}