using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Templates;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xpand.Extensions.ExpressionExtensions;
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


        [SuppressMessage("Usage", "ASP0006:Do not use non-literal sequence numbers")]
        public static void AddComponent(this RenderTreeBuilder builder, object component,int sequence=0) {
            builder.OpenComponent(sequence,component.GetType());
            builder.AddMultipleAttributes(component);
            builder.CloseComponent();
        }

        
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
            => frameTemplate.ProcessEvent(nameof(FrameTemplate.ViewChanged)).To(frameTemplate);
        
        public static IObservable<TArgs> WhenCallback<TArgs>(this object source, string callbackName) 
            => new Subject<TArgs>().Use(subject => {
                source.SetPropertyValue(callbackName, EventCallback.Factory.Create<TArgs>(source, args => subject.OnNext(args)));
                return subject.AsObservable().Finally(subject.Dispose);
            });
        public static IObservable<TMemberValue> WhenCallback<TObject,TMemberValue>(this TObject source, Expression<Func<TObject, EventCallback<TMemberValue>>> callbackName) 
            => source.WhenCallback<TMemberValue>(callbackName.MemberExpressionName());
    }
}
