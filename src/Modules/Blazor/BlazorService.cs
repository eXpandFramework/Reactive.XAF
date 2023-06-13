using System;
using System.Collections;
using System.Linq;
using System.Reactive;
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
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Blazor.Editors;
using Xpand.XAF.Modules.Blazor.Services;
using Xpand.XAF.Modules.Reactive.Services;
using NestedFrameTemplate = Xpand.XAF.Modules.Blazor.Templates.NestedFrameTemplate;

namespace Xpand.XAF.Modules.Blazor {
    public static class BlazorService {
        internal static  IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.DefaultLookupPropertyEditor()
                .Merge(manager.MarkupContentPropertyEditor())
                .Merge(Observable.If(() => DesignerOnlyCalculator.IsRunTime,manager.Defer(() => manager.CheckBlazor(typeof(BlazorStartup).FullName, typeof(BlazorModule).Namespace))))
                .Merge(manager.WhenApplication(application => application.UseCustomNestedFrameTemplate()
                    .Merge(application.ApplyDxDataGridModel())).ToUnit())
            ;

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
            => frameTemplate.WhenEvent(nameof(FrameTemplate.ViewChanged)).To(frameTemplate);
    }
}
