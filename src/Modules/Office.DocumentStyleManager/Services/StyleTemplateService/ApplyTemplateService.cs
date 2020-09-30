using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Templates;
using DevExpress.XtraRichEdit;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService{
	public static class ApplyTemplateService{
		public static string ApplyTemplateStyleActionContainer = nameof(BusinessObjects.ApplyTemplateStyle);
		public static SimpleAction ApplyTemplate(this (DocumentStyleManagerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ApplyTemplate)).As<SimpleAction>();

        internal static IObservable<Unit> ApplyTemplateStyle(this ApplicationModulesManager manager)
            => manager.RegisterApplyTemplesAction().ApplyTemplate().ToUnit()
                .Merge(manager.ShowStyleTemplate())
                .Merge(manager.TemplateDocumentConnect())
                .Merge(manager.SynchronizeScrolling())
                .ToUnit();

        private static IObservable<Unit> SynchronizeScrolling(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenDetailViewCreated().ToDetailView()
                .SynchronizeScrolling<ApplyTemplateStyle>(style => style.Original, style => style.Changed));

        private static IObservable<TemplateDocument> ApplyTemplate(this IObservable<SimpleAction> source)
            => source.WhenExecute()
                .SelectMany(args => {
                    var detailView = args.Action.View<DetailView>();
                    var changedRichEditControl = detailView.ApplyTemplateStyleChangedRichEditControl();
                    var documentsListView = detailView
                        .GetListPropertyEditor<ApplyTemplateStyle>(style => style.Documents).ListView;
                    var applyTemplateStyle = ((ApplyTemplateStyle) detailView.CurrentObject);
                    return applyTemplateStyle.Documents
                        .Select(document => {
                            documentsListView.CurrentObject = document;
                            var sourceListView = detailView.Model.Application.Views[applyTemplateStyle.ListView].AsObjectView;
                            var templateListViewItem = detailView.Model.Application.DocumentStyleManager()
                                .ApplyTemplateListViews[sourceListView.Id];
                            using var objectSpace = args.Action.Application.CreateObjectSpace(sourceListView.ModelClass.TypeInfo.Type);
                            var sourceObject = objectSpace.GetObjectByKey(sourceListView.ModelClass.TypeInfo.Type, document.Key);
                            templateListViewItem.Content.MemberInfo.SetValue(sourceObject, changedRichEditControl.Document.ToByteArray(DocumentFormat.OpenXml));
                            templateListViewItem.TimeStamp?.MemberInfo.SetValue(sourceObject, DateTime.Now);
                            objectSpace.CommitChanges();
                            return document;
                        })
                        .ToObservable(ImmediateScheduler.Instance)
                        .TraceDocumentStyleModule(document => document.Name);
                });

		internal static ListView ChangedStylesListView(this DetailView detailView) 
            => detailView.GetListPropertyEditor<ApplyTemplateStyle>(style => style.ChangedStyles).ListView;

		internal static ListView DocumentsListView(this DetailView detailView) 
            => detailView.GetListPropertyEditor<ApplyTemplateStyle>(style => style.Documents).ListView;

		internal static IRichEditDocumentServer ApplyTemplateStyleChangedRichEditControl(this DetailView detailView) 
            => detailView.GetPropertyEditor<ApplyTemplateStyle>(o => o.Changed).RichEditControl();

		internal static IRichEditDocumentServer ApplyTemplateOriginalRichEditControl(this DetailView detailView) 
            => detailView.GetPropertyEditor<ApplyTemplateStyle>(o => o.Original).RichEditControl();

        private static IObservable<SimpleAction> RegisterApplyTemplesAction(this ApplicationModulesManager manager)
            => manager.RegisterViewSimpleAction(nameof(ApplyTemplate), action => {
                    action.TargetViewType = ViewType.DetailView;
                    action.TargetObjectType = typeof(ApplyTemplateStyle);
                    action.Caption = "Save Changes";
                    action.Category = ApplyTemplateStyleActionContainer;
                    action.PaintStyle = ActionItemPaintStyle.CaptionAndImage;
                    action.ImageName = "ChangeFontStyle";
                })
                .Publish().RefCount();
	}
}