using System;
using System.Collections;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Templates;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Blazor.Editors;
using Xpand.XAF.Modules.Blazor.Services;
using Xpand.XAF.Modules.Reactive.Services;
using AssemblyExtensions = Xpand.Extensions.AssemblyExtensions.AssemblyExtensions;
using NestedFrameTemplate = Xpand.XAF.Modules.Blazor.Templates.NestedFrameTemplate;

namespace Xpand.XAF.Modules.Blazor {
    public static class BlazorService {
        internal static  IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.DefaultLookupPropertyEditor()
                .Merge(manager.MarkupContentPropertyEditor())
                .Merge(manager.CheckBlazor1(typeof(BlazorStartup).FullName, typeof(BlazorModule).Namespace))
                .Merge(manager.WhenApplication(application => application.UseCustomNestedFrameTemplate()
                    .Merge(application.ApplyDxDataGridModel())).ToUnit());
        
        
        public static IObservable<Unit> CheckBlazor1(this ApplicationModulesManager manager, string hostingStartupType, string requiredPackage) 
	        => manager.WhereApplication().ToObservable().Where(_ => DesignerOnlyCalculator.IsRunTime)
		        .SelectMany(application => new[] {(hostingStartupType, requiredPackage), ("Xpand.Extensions.Blazor.HostingStartup", "Xpand.Extensions.Blazor")
		        }.ToObservable().SelectMany(t => application.CheckBlazor1(t.Item1, t.Item2)));


        public static IObservable<Unit> CheckBlazor1(this XafApplication xafApplication, string hostingStartupType, string requiredPackage) {
	        if (xafApplication.GetPlatform() == Platform.Blazor) {
		        var startup = AssemblyExtensions.EntryAssembly.Attributes()
			        .Where(attribute => attribute.IsInstanceOf("Microsoft.AspNetCore.Hosting.HostingStartupAttribute"))
			        .Where(attribute => ((Type) attribute.GetPropertyValue("HostingStartupType")).FullName == hostingStartupType);
		        if (!startup.Any()) {
			        throw new InvalidOperationException($"Install the {requiredPackage} package in the front end project and add: [assembly: HostingStartup(typeof({hostingStartupType}))]");
		        }
	        }
	        return Observable.Empty<Unit>();
        }
        private static IObservable<Unit> UseCustomNestedFrameTemplate(this XafApplication application) 
            => application.WhenCreateCustomTemplate().Where(t => t.e.Context==TemplateContext.NestedFrame)
                .Do(t => t.e.Template=new NestedFrameTemplate()).ToUnit();


        public static void AddComponent(this RenderTreeBuilder builder, object component,int sequence=0) {
            builder.OpenComponent(sequence,component.GetType());
            builder.AddMultipleAttributes(component);
            builder.CloseComponent();
        }

        public static T GetService<T>(this XafApplication application) => application.ToBlazor().ServiceProvider.GetService<T>();
        public static T GetRequiredService<T>(this XafApplication application) => application.ToBlazor().ServiceProvider.GetRequiredService<T>();

        public static BlazorApplication ToBlazor(this XafApplication application) => (BlazorApplication)application;

        

        public static void AddMultipleAttributes(this RenderTreeBuilder builder,object component) {
            var propertyInfos = component.GetType().Properties(Flags.InstancePublic).Where(info => info.Attributes<ParameterAttribute>().Any());
            foreach (var propertyInfo in propertyInfos) {
                var value = propertyInfo.GetValue(component);
                if (!value.IsDefaultValue()&&!propertyInfo.PropertyType.IsArray&&
                    (!typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType)||propertyInfo.PropertyType==typeof(string))) {
                    builder.AddAttribute(1,propertyInfo.Name,value);
                }
            }
        }

        public static IObservable<FrameTemplate> WhenViewChanged(this FrameTemplate frameTemplate) 
            => Observable.FromEventPattern<EventHandler,EventArgs>(h => frameTemplate.ViewChanged+=h,h => frameTemplate.ViewChanged-=h,Scheduler.Immediate)
                .TransformPattern<FrameTemplate>();
    }
}
