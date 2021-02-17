using System;
using System.Collections;
using System.Linq;
using System.Reactive;
using DevExpress.ExpressApp;
using Fasterflect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor {
    public static class BlazorService {
        internal static  IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.CheckBlazor(typeof(BlazorStartup).FullName, typeof(BlazorModule).Namespace).ToUnit();

        public static void AddComponent(this RenderTreeBuilder builder, object component,int sequence=0) {
            builder.OpenComponent(sequence,component.GetType());
            builder.AddMultipleAttributes(component);
            builder.CloseComponent();
        }

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

    }
}
