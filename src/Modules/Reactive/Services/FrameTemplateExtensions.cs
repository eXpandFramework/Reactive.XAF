using System;
using System.Reactive.Linq;
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
			=> frameTemplate.ReturnObservable().Where(_ => frameTemplate != null && frameTemplate.GetType().InheritsFrom("System.Windows.Forms.Form")).WhenNotDefault()
				.Select(template => new WindowsForm(template));
        

		public static IObservable<IWindowsForm> When(this IObservable<IWindowsForm> source, string eventName) 
			=> source.SelectMany(form => Observable.FromEventPattern(form.Template,eventName).To(form));
		
	}
}