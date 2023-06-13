using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Templates;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
	public interface IWindowsForm{
		IFrameTemplate Template{ get; }	
	}

	class WindowsForm:IWindowsForm{
		public IFrameTemplate Template{ get; }

		public WindowsForm(IFrameTemplate template){
			Template = template;
		}
	}
	public static class FrameTemplateExtensions{
		public static IObservable<IWindowsForm> WhenWindowsForm(this IFrameTemplate frameTemplate) 
			=> frameTemplate.Observe().Where(_ => frameTemplate != null && frameTemplate.GetType().InheritsFrom("System.Windows.Forms.Form")).WhenNotDefault()
				.Select(template => new WindowsForm(template));


        public static IObservable<Window> WhenWindowTemplate(this XafApplication application,TemplateContext templateContext=default)
            => application.WhenFrameCreated(templateContext==default?TemplateContext.ApplicationWindow:templateContext)
                .SelectMany(frame => frame.WhenTemplateChanged()).Cast<Window>();

		public static IObservable<IWindowsForm> When(this IObservable<IWindowsForm> source, params string[] eventNames) 
			=> source.SelectMany(form => eventNames.ToNowObservable().SelectMany(eventName => form.Template.WhenEvent(eventName).To(form)));
		
	}
}