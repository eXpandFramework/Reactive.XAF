using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.SystemModule;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Windows {
    static class ExitService {
        public static IObservable<Window> OnWindowEscape(this IObservable<Window> source)
            => source.Cast<WinWindow>().MergeIgnored(window => window.Template
                .WhenEvent<Form, KeyEventArgs>(nameof(WinWindow.KeyDown)).Where(t => t.args.KeyCode == Keys.Escape).To(window)
                .Do(model => model.OnEscape.CloseWindow,model => model.OnEscape.MinimizeWindow)
                .To(window),window => window.Application.Model().Exit.OnEscape.CloseWindow);

        static IObservable<Window> Do(this IObservable<Window> source, Func<IModelWindowsExit, bool> closeWindow,
            Func<IModelWindowsExit, bool> minimizeWindow)
            => source.Do(window => {
                var model = window.Application.Model().Exit;
                if (closeWindow(model)) {
                    window.Close();
                }

                if (minimizeWindow(model)) {
                    ((Form) window.Template).WindowState = FormWindowState.Minimized;
                }
            });

        public static IObservable<Window> OnWindowDeactivation(this IObservable<Window> source)
            => source.MergeIgnored(window => window.Template
                .WhenEvent<Form, EventArgs>(nameof(Form.Deactivate))
                .Do(_ => window.Close())
                .To(window),window => window.Application.Model().Exit.OnDeactivation.CloseWindow);

        public static IObservable<Window> CancelExit(this IObservable<Window> source) 
            => source.MergeIgnored(window => window.Template.WhenEvent<Form, FormClosingEventArgs>(nameof(Form.FormClosing))
                .TakeUntil(window.WhenCustomExit())
                .Select(t => {
                    t.args.Cancel = window.CanExit();
                    return (t.source, window.Application.Model().Exit);
                }).ChangeFormState().To(window));

        private static bool CanExit(this Window window) {
            var application = window.Application;
            var modelWindowsExit = window.Application.Model().Exit;
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
            => frame.GetController<EditModelController>().EditModelAction.WhenExecuting().ToUnit()
                .Merge(frame.Application.WhenLoggingOff().ToUnit())
                .Merge(frame.Application.WhenExiting().ToUnit())
        ;
    }
}