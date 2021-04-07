using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(object), EditorAliases.MarkupContent,false)]
    public class MarkupContentPropertyEditor : ComponentPropertyEditor {
        public MarkupContentPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }

        protected override RenderFragment CreateViewComponentCore(object dataContext) 
            => builder => Render(builder,MemberInfo.GetValue(dataContext));

        protected override RenderFragment RenderComponent() 
            => builder => Render(builder, PropertyValue);

        private void Render(RenderTreeBuilder builder,object propertyValue) {

            var markupContent = $@"
<div class=""dxbs-fl-ctrl""><!--!-->
    <div data-item-name=""{MemberInfo.Name}"" class=""d-none""></div><!--!-->
    <!--!-->{$"{propertyValue}".StringFormat(Model.DisplayFormat)}<!--!-->
</div>
";
            
            builder.AddMarkupContent(0, markupContent);
        }
    }

    public static class MarkupContentPropertyEditorService {
    
        internal static IObservable<Unit> MarkupContentPropertyEditor(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelBOModel>().SelectMany().Cast<IModelClass>()
                .SelectMany(modelClass => modelClass.OwnMembers.ToObservable())
                .UrlPropertyAttribute().ImgPropertyAttribute()
                .ToUnit()
                .Merge(manager.WhenGeneratingModelNodes<IModelViews>().SelectMany()
                    .ReaOnlyViewAttribute());

        private static IObservable<Unit> ReaOnlyViewAttribute(this IObservable<IModelView> source)
            => source.OfType<IModelDetailView>().ConcatIgnored(view => view.ModelClass.TypeInfo
                    .FindAttributes<ReadOnlyObjectViewAttribute>().Where(attribute => !attribute.AllowEdit)
                    .SelectMany(_ => view.MemberViewItems().Where(item => !item.ModelMember.MemberInfo.IsList)
                        .Execute(item => item.PropertyEditorType = typeof(MarkupContentPropertyEditor)))
                    .ToObservable())
                .ToUnit();
                

        private static IObservable<IModelMember> UrlPropertyAttribute(this IObservable<IModelMember> source)
            => source.ConcatIgnored(member => member.MemberInfo.FindAttributes<UrlPropertyAttribute>()
                .Do(_ => {
                    member.PropertyEditorType = typeof(MarkupContentPropertyEditor);
                    member.DisplayFormat = "<a href='{0}' target='_blank'>{0}</a>";
                }).ToObservable());

        private static IObservable<IModelMember> ImgPropertyAttribute(this IObservable<IModelMember> source) 
            => source.ConcatIgnored(member => member.MemberInfo.FindAttributes<ImgPropertyAttribute>()
                .Do(attribute => {
                    member.PropertyEditorType = typeof(MarkupContentPropertyEditor);
                    var imgSrc = "<img src='{0}'/>";
                    member.DisplayFormat= attribute.Width > 0 ? imgSrc.Replace("/>", $" width='{attribute.Width}' />") : imgSrc;
                    if (attribute.DetailViewWidth > 0) {
                        member.ModelClass.DefaultDetailView.Items.OfType<IModelPropertyEditor>()
                                .First(editor => editor.ModelMember == member).DisplayFormat =
                            imgSrc.Replace("/>", $" width='{attribute.DetailViewWidth}' />");
                    }
                }).ToObservable());
    }
}