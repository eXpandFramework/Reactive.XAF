using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Windows {
    public static class NotifyIconService {
        private static readonly Subject<NotifyIcon> NotifyIconSubject = new();

        public static IObservable<NotifyIcon> NotifyIconUpdated => NotifyIconSubject.AsObservable();

        internal static IObservable<Window> NotifyIcon(this IObservable<Window> source)
            => source.MergeIgnored(NotifyIcon);

        private static IObservable<Frame> NotifyIcon(Window frame) 
            => Observable.Using(() => new Container(), container => {
                if (frame.Model().NotifyIcon.Enabled) {
                    var notifyIcon = new NotifyIcon(container)
                        {Visible = true, ContextMenuStrip = new ContextMenuStrip(container)};
                    notifyIcon.SetupIcon();
                    return notifyIcon.ExecuteMenuItems(frame)
                        .Merge(notifyIcon.ShowOnDoubleClick(frame))
                        .Merge(frame.Application.WhenExiting()
                            .Do(_ => notifyIcon.Dispose())
                            .To(frame));
                }
                return Observable.Empty<Frame>();
            }).TraceWindows();

        private static void SetupIcon(this NotifyIcon icon) {
            string path = $"{AppDomain.CurrentDomain.ApplicationPath()}\\ExpressApp.ico";
            if (File.Exists(path))
                icon.Icon = new Icon(path);
            var resourceStream =
                typeof(AppDomainExtensions).Assembly.GetManifestResourceStream("Xpand.Extensions.Resources.ExpressApp.ico");
            if (resourceStream != null) icon.Icon = new Icon(resourceStream);
        }

        private static IObservable<Frame> ShowOnDoubleClick(this NotifyIcon notifyIcon, Frame frame) 
            => notifyIcon.WhenEvent(nameof(notifyIcon.DoubleClick)).Where(_ => frame.Model().NotifyIcon.ShowOnDblClick)
                .Do(_ => ((Form) frame.Application.MainWindow.Template).Show()).To(frame).TraceWindows();

        private static IObservable<Frame> ExecuteMenuItems(this NotifyIcon notifyIcon,Frame frame) 
            => notifyIcon.ContextMenuStrip.UpdateMenuItems(frame).Finally(() => NotifyIconSubject.OnNext(notifyIcon))
                .SelectMany(item => item.WhenEvent(nameof(item.Click)).ObserveOn(SynchronizationContext.Current!).To(item))
                .Do(item => {
                    var model = frame.Model().NotifyIcon;
                    var mainForm = ((Form) frame.Application.MainWindow.Template);
                    if (item.Text == model.LogOffText) {
                        frame.Application.LogOff();
                    }
                    if (item.Text == model.ExitText) {
                        frame.Application.Exit();
                    }
                    if (item.Text == model.ShowText) {
                        mainForm.Show();
                    }
                    if (item.Text == model.HideText) {
                        mainForm.Hide();
                    }
                }).To(frame).TraceWindows();

        private static IObservable<ToolStripMenuItem> UpdateMenuItems(this ContextMenuStrip strip, Frame frame) {
            strip.Items.Clear();
            var model = frame.Model().NotifyIcon;
            if (!string.IsNullOrEmpty(model.ShowText)) {
                strip.Items.Add(new ToolStripMenuItem(model.ShowText));
            }

            if (!string.IsNullOrEmpty(model.HideText)) {
                strip.Items.Add(new ToolStripMenuItem(model.HideText));
            }

            if (!string.IsNullOrEmpty(model.LogOffText)) {
                var logoffAction = frame.GetController<LogoffController>().LogoffAction;
                if (logoffAction.Active) {
                    strip.Items.Add(new ToolStripMenuItem(model.LogOffText));
                }
            }

            if (!string.IsNullOrEmpty(model.ExitText)) {
                strip.Items.Add(new ToolStripMenuItem(model.ExitText));
            }
            
            return strip.Items.Cast<ToolStripMenuItem>().ToObservable();
        }

    }
}