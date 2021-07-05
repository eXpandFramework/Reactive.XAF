using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Core;
using Fasterflect;
using HarmonyLib;
using Moq;
using Moq.Protected;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.TestsLib {
    public static class Extensions {
        public static IObservable<ConfirmationDialogClosedEventArgs> WhenConfirmationDialogClosed(this Messaging messaging)
            => Observable
                .FromEventPattern<EventHandler<ConfirmationDialogClosedEventArgs>, ConfirmationDialogClosedEventArgs>(
                    h => Messaging.ConfirmationDialogClosed += h, h => Messaging.ConfirmationDialogClosed -= h,ImmediateScheduler.Instance)
                .Select(p => p.EventArgs);

        public static bool ShowDialog() => false;

        public static void PatchFormShowDialog(this XafApplication application) 
            => application.Patch(harmony => harmony.Patch(typeof(Form).Method(nameof(Form.ShowDialog), Type.EmptyTypes),
                new HarmonyMethod(typeof(Extensions), nameof(ShowDialog))));

        public static void MockMessaging(this Messaging messaging,Func<DialogResult> result) {
            var mock = new Mock<Messaging>(messaging.GetPropertyValue("Application"));
            mock.Protected()
                .Setup<DialogResult>("ShowCore", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<MessageBoxButtons>(), ItExpr.IsAny<MessageBoxIcon>())
                .Returns(result);
            WinApplication.Messaging=mock.Object;
        }

    }
}