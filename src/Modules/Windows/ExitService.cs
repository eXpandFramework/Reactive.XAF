using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.SystemModule;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Windows {
    static class ExitService {
        public static IObservable<Window> OnWindowEscape(this IObservable<Window> source)
            => source.Cast<WinWindow>().MergeIgnored(window => window
                .WhenEvent<WinWindow, KeyEventArgs>(nameof(WinWindow.KeyDown)).Where(t => t.args.KeyCode == Keys.Escape).To(window)
                .TakeUntil(window.WhenDisposedFrame())
                .OnWindowEscape(model => model.OnEscape.CloseWindow,model => model.OnEscape.MinimizeWindow,model => model.OnEscape.ExitApplication)
                .To(window),window => {
                var onEscape = window.Model().Exit.OnEscape;
                return onEscape.CloseWindow||onEscape.ExitApplication;
            });

        static IObservable<Window> OnWindowEscape(this IObservable<Window> source, Func<IModelWindowsExit, bool> closeWindow,
            Func<IModelWindowsExit, bool> minimizeWindow,Func<IModelWindowsExit, bool> exitApplication)
            => source.WhenNotDefault(window => window.Application)
                .TraceWindows()
                .Do(window => {
                    var model = window.Model().Exit;
                    if (closeWindow(model)) {
                        window.Close();
                    }

                    if (minimizeWindow(model)) {
                        ((Form) window.Template).WindowState = FormWindowState.Minimized;
                    }

                    if (exitApplication(model)) {
                        window.Application.Exit();
                    }
                });

        public static IObservable<Window> OnWindowDeactivation(this IObservable<Window> source,Func<Window,bool> apply=null)
            => source.MergeIgnored(window => window.Template
	            .WhenEvent<Form, EventArgs>(nameof(Form.Activated)).TakeUntil(window.WhenDisposedFrame())
                .Select(_ => window.Template.WhenEvent<Form, EventArgs>(nameof(Form.Deactivate))).Switch()
                .WhenNotDefault(_ => window.Application)
                .TraceWindows(_ => $"{nameof(IModelOn.CloseWindow)}={window.Model().Exit.OnDeactivation.CloseWindow}")
                .Do(_ => {
                    if (window.Model().Exit.OnDeactivation.CloseWindow) {
                        window.Close();
                    }
                    else {
                        window.Application.Exit();
                    }
                })
                .To(window),window => {
                var onDeactivation = window.Model().Exit.OnDeactivation;
                return (onDeactivation.CloseWindow || onDeactivation.ExitApplication) && (apply == null || apply(window));
            });

        public static IObservable<Window> CancelExit(this IObservable<Window> source) 
            => source.MergeIgnored(window => window.Template.WhenEvent<Form, FormClosingEventArgs>(nameof(Form.FormClosing))
                    .TakeUntil(window.WhenDisposedFrame())
                .TakeUntil(window.WhenCustomExit())
                .Select(t => {
                    t.args.Cancel = window.CanExit();
                    return (t.source, window.Model().Exit);
                }).ChangeFormState().To(window))
                .TraceWindows();

        public static IObservable<Window> ExitAfterModelEdit(this IObservable<Window> source) 
            => source.MergeIgnored(ExitAfterModelEdit,window => window.Model().Exit.ExitAfterModelEdit);

        private static IObservable<SimpleActionExecuteEventArgs> ExitAfterModelEdit(Window window) {
            var application = window.Application;
            return window.GetController<EditModelController>().EditModelAction.WhenExecuteCompleted()
                .Do(_ => application.Exit()).TraceWindows();
        }

        private static bool CanExit(this Window window) {
            var application = window.Application;
            var modelWindowsExit = window.Model().Exit;
            var prompt = modelWindowsExit.Prompt;
            return prompt.Enabled
                ? WinApplication.Messaging.GetUserChoice(
                      prompt.Message.StringFormat(application.Title),
                      prompt.Title.StringFormat(application.Title), MessageBoxButtons.YesNo) !=
                  DialogResult.Yes
                : modelWindowsExit.OnExit.HideMainWindow || modelWindowsExit.OnExit.MinimizeMainWindow;
        }

        private static IObservable<Form> ChangeFormState(this IObservable<(Form form, IModelWindowsExit model)> source)
            => source.Select(t => {
                if (t.model.OnExit.HideMainWindow) {
                    t.form.Hide();
                }
                if (t.model.OnExit.MinimizeMainWindow) {
                    t.form.WindowState = FormWindowState.Minimized;
                }
                return t.form;
            });


        private static IObservable<Unit> WhenCustomExit(this Frame frame)
            => frame.GetController<EditModelController>().EditModelAction.WhenExecuting(args => Observable.Empty<ActionBase>()).ToUnit()
                .Merge(frame.Application.WhenLoggingOff().ToUnit())
                .Merge(frame.Application.WhenExiting().ToUnit())
        ;
    }
}