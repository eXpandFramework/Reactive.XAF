using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors.Grid;
using DevExpress.ExpressApp.Blazor.Templates;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Fasterflect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Blazor.Editors;
using Xpand.XAF.Modules.Blazor.Model;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using LookupPropertyEditor = DevExpress.ExpressApp.Blazor.Editors.LookupPropertyEditor;
using NestedFrameTemplate = Xpand.XAF.Modules.Blazor.Templates.NestedFrameTemplate;

namespace Xpand.XAF.Modules.Blazor {
    public static class BlazorService {
        internal static  IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.DefaultLookupPropertyEditor()
                .Merge(manager.UrlPropertyAttribute())
                .Merge(manager.CheckBlazor(typeof(BlazorStartup).FullName, typeof(BlazorModule).Namespace))
                .Merge(manager.WhenApplication(application => application.UseCustomNestedFrameTemplate()
                    .Merge(application.ApplyDxDataGridModel())).ToUnit());


        static IObservable<Unit> DefaultLookupPropertyEditor(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelBOModel>().SelectMany()
                .SelectMany(mClass => mClass.OwnMembers
                    .Where(member => member.PropertyEditorType == typeof(LookupPropertyEditor))
                    .Do(member => member.PropertyEditorType = typeof(Editors.LookupPropertyEditor))
                ).ToUnit();

        static IObservable<Unit> UrlPropertyAttribute(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => t.e.TypesInfo.PersistentTypes.SelectMany(info => info.Members.ToArray()
                    .SelectMany(memberInfo => memberInfo.AddMarkupPropertyEditorAttributes<UrlPropertyAttribute>(_ => "<a href='{0}'>{0}</a>").Cast<Attribute>()
                        .Concat(memberInfo.AddMarkupPropertyEditorAttributes<ImgPropertyAttribute>(attribute => {
                            var imgSrc = "<img src='{0}'/>";
                            if (attribute.Width > 0) {
                                imgSrc = imgSrc.Replace("/>", $" width='{attribute.Width}' />");
                            }
                            return imgSrc;
                        })))))
                .ToUnit();

        private static IEnumerable<T> AddMarkupPropertyEditorAttributes<T>(this IMemberInfo memberInfo,Func<T,string> valueFactory) where T:Attribute
            => memberInfo.FindAttributes<T>().ToArray()
                .Execute(_ => {
                    var value=valueFactory.Invoke(_);
                    memberInfo.AddAttribute(new EditorAliasAttribute(nameof(MarkupContentPropertyEditor)));
                    memberInfo.AddAttribute(new ModelDefaultAttribute("DisplayFormat",value));
                });

        private static IObservable<Unit> ApplyDxDataGridModel(this XafApplication application) 
            => application.WhenFrameViewControls().WhenFrame(viewType: ViewType.ListView)
                .DxDataGridModel().Apply();

        private static IObservable<Unit> Apply(this IObservable<(Frame frame, IModelListViewFeature feature)> source) {
            var propertyInfos=new HashSet<string>(typeof(IModelDxDataGridModel).GetProperties().Select(info => info.Name));
            return source.SelectMany(t => {
                    var dxDataGridModel = ((GridListEditor) t.frame.View.AsListView().Editor).GetDataGridAdapter().DataGridModel;
                    return propertyInfos.Select(s => (name:s,value:t.feature.DxDataGridModel.GetValue(s))).Where(t3 => t3.value!=null)
                        .Select(t1 =>( t1.name,t1.value,t.frame))
                        .Do(t2 => dxDataGridModel.SetPropertyValue(t2.name, t2.value));
                })
                
                .ToUnit();
        }

        private static IObservable<(Frame frame, IModelListViewFeature feature)> DxDataGridModel(this IObservable<Frame> source) 
            => source.SelectMany(frame => frame
                .FrameFeatures().Where(t => (t.feature.IsPropertyVisible(nameof(IModelListViewFeature.DxDataGridModel))))
                .Select(t => (frame,t.feature)));

        private static IEnumerable<(Frame frame, IModelListViewFeature feature)> FrameFeatures(this Frame frame) 
            => frame.Application.Model.ToReactiveModule<IModelReactiveModulesBlazor>().Blazor.ListViewFeatures.Where(feature => feature.ListView==frame.View.Model)
                .Select(feature => (frame,feature));

        private static IObservable<Unit> UseCustomNestedFrameTemplate(this XafApplication application) 
            => application.WhenCreateCustomTemplate().Where(t => t.e.Context==TemplateContext.NestedFrame)
                .Do(t => t.e.Template=new NestedFrameTemplate()).ToUnit();


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

        public static IObservable<FrameTemplate> WhenViewChanged(this FrameTemplate frameTemplate) 
            => Observable.FromEventPattern<EventHandler,EventArgs>(h => frameTemplate.ViewChanged+=h,h => frameTemplate.ViewChanged-=h,Scheduler.Immediate)
                .TransformPattern<FrameTemplate>();
    }
}
