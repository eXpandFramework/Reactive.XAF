using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.XtraRichEdit.API.Native;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService{
	public static class TemplateDocumentService{
		internal static IObservable<Unit> TemplateDocumentConnect(this ApplicationModulesManager manager) 
            => manager.WhenTemplateDocumentListViewOnFrame(nestedFrame => nestedFrame.WhenContentApplied().ApplyTemplateStyles().ToUnit());

        private static IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> WhenContentApplied(
            this IObservable<NestedFrame> source)
            => source.WhenDocumentSelectionChanged()
                .ApplyOriginalContent()
                .WhenNotDefault(t => t.templateDocument.ApplyStyleTemplate.Template)
                .TraceDocumentStyleModule(t => t.templateDocument.ApplyStyleTemplate.Template.Name)
                .Publish().RefCount();

		private static IObservable<NestedFrame> WhenDocumentSelectionChanged(this IObservable<NestedFrame> source) 
            => source.SelectMany(frame => frame.View.WhenSelectionChanged().To(frame)).Cast<NestedFrame>().TraceDocumentStyleModule(frame => frame.View.Id);

        private static IObservable<Unit> WhenTemplateDocumentListViewOnFrame(this ApplicationModulesManager manager,
            Func<IObservable<NestedFrame>, IObservable<Unit>> nestedFrame)
            => manager.WhenApplication(application => nestedFrame(application
                .WhenViewOnFrame(typeof(TemplateDocument), ViewType.ListView, Nesting.Nested)
                .Cast<NestedFrame>()));

        private static IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> RemoveUnusedStyles(
            this IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> source)
            => source.WhenDefault(_ => _.templateDocument.ApplyStyleTemplate.Template.KeepUnused)
                .SelectMany(t => {
                    var detailView = t.nestedFrame.ViewItem.View.AsDetailView();
                    var richEditControl = detailView.ApplyTemplateStyleChangedRichEditControl();
                    var unUsed = richEditControl.Document.UnusedStyles().ToArray();
                    richEditControl.Document.DeleteStyles(unUsed);
                    return t.ReturnObservable().TraceDocumentStyleModule(_ => unUsed.Select(style => style.StyleName).Join(", "));
                });

        private static IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> EnsureStyles(
            this IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> source, Document defaultPropertiesProvider)
            => source.Do(t => {
                    var detailView = t.nestedFrame.ViewItem.View.AsDetailView();
                    var template = t.templateDocument.ApplyStyleTemplate.Template;
                    var richEditControl = detailView.ApplyTemplateStyleChangedRichEditControl();
                    var usedStyles = richEditControl.Document.UsedStyles().ToArray();
                    var documentStyleLinks = template.DocumentStyleLinks.Where(link => link.Operation == DocumentStyleLinkOperation.Ensure);
                    foreach (var link in documentStyleLinks){
                        link.SetDefaultPropertiesProvider(defaultPropertiesProvider);
                        var ensureStyle = link.ReplacementStyle.Ensure(richEditControl.Document, usedStyles,
                            defaultPropertiesProvider);
                        if (ensureStyle){
                            link.Count = 1;
                            t.templateDocument.ApplyStyleTemplate.ChangedStyles.Add(link);
                        }
                    }
                })
                .TraceDocumentStyleModule(t => $"{t.nestedFrame.View.Id}, {t.templateDocument.Name}");

        private static IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> ReplaceStyles(
            this IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> source, Document defaultPropertiesProvider)
            => source.Select(t => {
                var detailView = t.nestedFrame.ViewItem.View.AsDetailView();
                var template = t.templateDocument.ApplyStyleTemplate.Template;
                var richEditControl = detailView.ApplyTemplateStyleChangedRichEditControl();
                foreach (var link in template.DocumentStyleLinks.Where(link =>
                    link.Operation == DocumentStyleLinkOperation.Replace)){
                    link.SetDefaultPropertiesProvider(defaultPropertiesProvider);
                    var replacement = link.ReplacementStyle;
                    var documentStyle = link.OriginalStyle;
                    var characterProperties = richEditControl.Document.ReplaceStyles(replacement, defaultPropertiesProvider, documentStyle);
                    link.Count = characterProperties.Length;
                    t.templateDocument.ApplyStyleTemplate.ChangedStyles.Add(link);
                }

                return t;
            }).TraceDocumentStyleModule(t => $"{t.nestedFrame.View.Id}, {t.templateDocument.Name}");


        private static IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> ApplyTemplateStyles(
            this IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> source)
            => source.Do(t => t.templateDocument.ApplyStyleTemplate.ChangedStyles.Clear())
                .SelectMany(t => t.nestedFrame.Application
                    .DefaultPropertiesProvider(document => t.ReturnObservable()
                        .ReplaceStyles(document)
                        .RemoveUnusedStyles()
                        .EnsureStyles(document))
                ).TraceDocumentStyleModule(t => $"{t.nestedFrame.View.Id}, {t.templateDocument.Name}");

        private static IObservable<(NestedFrame nestedFrame, TemplateDocument templateDocument)> ApplyOriginalContent(
            this IObservable<NestedFrame> source) 
            => source.SelectMany(frame => frame.Application.UseObjectSpace(space => {
                    var view = frame.View.AsListView();
                    var templateDocument = (TemplateDocument) view.CurrentObject;
                    if (templateDocument != null){
                        var sourceListView = view.Model.Application.Views[templateDocument.ApplyStyleTemplate.ListView].AsObjectView;
                        var contentMemberInfo = view.Model.Application.DocumentStyleManager()
                            .ApplyTemplateListViews[sourceListView.Id].Content.MemberInfo;
                        
                        var sourceObject = space.GetObjectByKey(sourceListView.ModelClass.TypeInfo.Type, templateDocument.Key);
                        var value = contentMemberInfo.GetValue(sourceObject);
                        if (value != null){
                            templateDocument.ApplyStyleTemplate.Original = (byte[]) value;
                            templateDocument.ApplyStyleTemplate.Changed = (byte[]) value;
                        }
                        return (frame, templateDocument).ReturnObservable();
                    }
                    return default;
                }))
                .WhenNotDefault()
                .TraceDocumentStyleModule(t => $"{t.frame.View.Id}, {t.templateDocument.Name}")
        ;
    }
}