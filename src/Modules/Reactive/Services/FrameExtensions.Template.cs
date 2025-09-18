using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Templates;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public interface IWindowsForm{
        IFrameTemplate Template{ get; }	
    }

    class WindowsForm(IFrameTemplate template) : IWindowsForm {
        public IFrameTemplate Template{ get; } = template;
    }
    public static class FrameTemplateExtensions{
        public static IObservable<IWindowsForm> WhenWindowsForm(this IFrameTemplate frameTemplate) 
            => frameTemplate.Observe().Where(_ => frameTemplate != null && frameTemplate.GetType().InheritsFrom("System.Windows.Forms.Form")).WhenNotDefault()
                .Select(template => new WindowsForm(template)).PushStackFrame();

        public static IObservable<Window> WhenWindowTemplate(this XafApplication application,TemplateContext templateContext=default)
            => application.WhenFrameCreated(templateContext==default?TemplateContext.ApplicationWindow:templateContext)
                .If(frame => frame.Context==TemplateContext.ApplicationWindow&&frame.Template!=null,frame => frame.Observe(),frame => frame.WhenTemplateChanged())
                .Cast<Window>().PushStackFrame();

        public static IObservable<IWindowsForm> When(this IObservable<IWindowsForm> source, params string[] eventNames) 
            => source.SelectMany(form => eventNames.ToNowObservable().SelectMany(eventName => form.Template.ProcessEvent(eventName).To(form))).PushStackFrame();
		
    }
}