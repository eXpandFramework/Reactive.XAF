using System;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public interface IXAFAppWinAPI{
        XafApplication Application{ get; }
    }

    class XAFAppWinAPI:IXAFAppWinAPI{
        public XafApplication Application{ get; }

        public XAFAppWinAPI(XafApplication application){
            Application = application;
        }

    }

    public static class XAFWinApiExtensions{
        public static IObservable<IXAFAppWinAPI> WhenWin(this XafApplication application) 
            => application.GetPlatform() == Platform.Win ? new XAFAppWinAPI(application).Observe() : Observable.Empty<IXAFAppWinAPI>();

        public static IObservable<Window> WhenMainFormShown(this IXAFAppWinAPI api) 
            => api.Application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
                .SelectMany(window => window.Template.WhenWindowsForm().When("Shown").To(window));

        public static IObservable<Window> WhenMainFormVisible(this IXAFAppWinAPI api) 
            => api.Application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
                .SelectMany(window => window.WhenTemplateChanged().Select(window1 => window1.Template).StartWith(window.Template).WhenNotDefault()
                    .SelectMany(template => template.WhenWindowsForm().When("VisibleChanged")).To(window));

        public static IObservable<(HandledEventArgs handledEventArgs, Exception exception, Exception originalException)> WhenCustomHandleException(this IObservable<IXAFAppWinAPI> source) 
            => source.SelectMany(api => Observable.FromEventPattern(api.Application, "CustomHandleException")
                .Select(pattern => (((HandledEventArgs) pattern.EventArgs), exception:((Exception) pattern.EventArgs.GetPropertyValue("Exception")
                    ),originalException:((Exception) pattern.EventArgs.GetPropertyValue("Exception")))));

    }
}