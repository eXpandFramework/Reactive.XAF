using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.XtraRichEdit.API.Native;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
    public static class TemplateStyleSelectionService{
        public static SingleChoiceAction TemplateStyleSelection(this (DocumentStyleManagerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(TemplateStyleSelection)).As<SingleChoiceAction>();

        internal static IObservable<Unit> LinkTemplate(this ApplicationModulesManager manager){
            var registerAction = manager.RegisterAction();
            return registerAction.ConfigureSelectionContext()
                    .Merge(registerAction.Execute())
                    .Merge(manager.EnableAction())
                .Merge(manager.AssignStyleLinkDocument());
        }

        private static IObservable<Unit> AssignStyleLinkDocument(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenDetailViewCreated().ToDetailView()
	            .When(typeof(BusinessObjects.DocumentStyleManager)).Publish().RefCount()
                .TraceDocumentStyleModule(view => view.Id)
	            .AssignStyleLinkDocument());


        private static IObservable<Unit> Execute(this IObservable<SingleChoiceAction> source) 
            => source.WhenExecute()
                .Do(e => {
                    var view = e.Action.View<DetailView>();
                    var template = ((BusinessObjects.DocumentStyleManager) view.CurrentObject).DocumentStyleLinkTemplate;
                    var objectSpace = ((IObjectSpaceLink) template).ObjectSpace;
                    var templateStyleMembers = objectSpace.GetTemplateStyleMembers();
                    var replacementStylesListView = view.ReplacementStylesListView();
                    var replacementStyle = replacementStylesListView.SelectedObjects.Cast<DocumentStyle>().First();
                    var replacementTemplateStyle = replacementStyle.NewTemplateStyle(objectSpace, templateStyleMembers);
                    var allStyles = view.AllStylesListView().SelectedObjects.Cast<IDocumentStyle>();
                    var document = view.DocumentManagerContentRichEditServer().Document;
                    var links = allStyles.DocumentStyleLinks(objectSpace, templateStyleMembers, replacementTemplateStyle,
                        document, ((DocumentStyleLinkOperation) e.SelectedChoiceActionItem.Data));

                    template.DocumentStyleLinks.AddRange(links);
                })
                .ToUnit();

        private static IEnumerable<DocumentStyleLink> DocumentStyleLinks(this IEnumerable<IDocumentStyle> source,
	        IObjectSpace objectSpace, IMemberInfo[] templateStyleMembers, TemplateStyle replacementTemplateStyle, Document document, DocumentStyleLinkOperation operation) 
            => source.Select(style => {
                    var link = objectSpace.CreateObject<DocumentStyleLink>();
                    if (operation == DocumentStyleLinkOperation.Replace){
	                    link.Original = style.NewTemplateStyle(objectSpace, templateStyleMembers);
	                    link.Replacement = replacementTemplateStyle;
                    }
                    else{
	                    link.Replacement = style.NewTemplateStyle(objectSpace, templateStyleMembers);
                    }
                    link.SetDefaultPropertiesProvider(document);
                    link.Operation=operation;
                    return link;
                });

        public static TemplateStyle NewTemplateStyle(this IDocumentStyle documentStyle, IObjectSpace objectSpace, IMemberInfo[] templateStyleMembers = null){
            templateStyleMembers ??= objectSpace.GetTemplateStyleMembers();
            var templateStyle = objectSpace.CreateObject<TemplateStyle>();
            foreach (var member in templateStyleMembers){
                var memberInfo = documentStyle.GetTypeInfo().FindMember(member.Name);
                if (memberInfo != null){
                    var value = memberInfo.GetValue(documentStyle);
                    if (value is IDocumentStyle style){
	                    var firstOrDefault = objectSpace.GetObjectsQuery<TemplateStyle>(true).FirstOrDefault(_ => _.StyleName == style.StyleName);
	                    value = firstOrDefault ?? style.NewTemplateStyle(objectSpace, templateStyleMembers);
                    }
                    member.SetValue(templateStyle, value);
                }
            }
            return templateStyle;
        }

        internal static IMemberInfo[] GetTemplateStyleMembers(this IObjectSpace objectSpace) 
            => objectSpace.TypesInfo.FindTypeInfo(typeof(TemplateStyle))
                .OwnMembers.Where(info => info.IsPersistent).ToArray();

        private static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager) 
            => manager.RegisterViewSingleChoiceAction(nameof(TemplateStyleSelection), action => {
                        var simpleAction = action.Configure();
                        simpleAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
                        simpleAction.ImageName = "PageSetup";
                        simpleAction.Caption = "Template Styles";
                        simpleAction.Items.Add(new ChoiceActionItem(DocumentStyleLinkOperation.Replace.ToString(), DocumentStyleLinkOperation.Replace));
                        simpleAction.Items.Add(new ChoiceActionItem(DocumentStyleLinkOperation.Ensure.ToString(), DocumentStyleLinkOperation.Ensure));
                        simpleAction.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                    })
                    .Publish().RefCount();

        private static IObservable<Unit> EnableAction(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenViewOnFrame(typeof(BusinessObjects.DocumentStyleManager), ViewType.DetailView)
	                .SelectMany(window => {
		                var templateStyleSelection = window.Action<DocumentStyleManagerModule>().TemplateStyleSelection();
                        var replaceStylesEnabled = window.Action<DocumentStyleManagerModule>().ReplaceStyles().Enabled;
                        var whenResultValueChanged = replaceStylesEnabled.WhenResultValueChanged();
		                return whenResultValueChanged
			                .Do(_ => templateStyleSelection.Enabled[nameof(ReplaceStylesService.ReplaceStyles)] =
				                _.e.NewValue)
                            .TraceDocumentStyleModule(_ => templateStyleSelection.Id);
	                })
                    .ToUnit());
    }
}