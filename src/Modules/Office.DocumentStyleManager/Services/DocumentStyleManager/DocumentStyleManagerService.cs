using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
	public static class DocumentStyleManagerService{
        
        public const string StyleManagerActionCategory = "StyleManager";
        internal static ListView AllStylesListView(this DetailView view) 
            =>  view.GetListPropertyEditor<BusinessObjects.DocumentStyleManager>(_ => _.AllStyles).Frame.View.AsListView();

        internal static IRichEditDocumentServer DocumentManagerContentRichEditServer(this DetailView view) 
            => view.GetPropertyEditor<PropertyEditor, BusinessObjects.DocumentStyleManager>(styleManager => styleManager.Content).RichEditControl();

        internal static ListView ReplacementStylesListView(this DetailView view) 
	        =>  view.GetListPropertyEditor<BusinessObjects.DocumentStyleManager>(_ => _.ReplacementStyles).Frame.View.AsListView();

        internal static IObservable<Unit> DocumentStyleManager(this ApplicationModulesManager manager)
            => manager.DocumentStyleManagerDetailView(view => view.WhenControlsCreated().FilterReplacementStyles())
                .Merge(manager.DocumentStyleLinkTemplate())
                .Merge(manager.SynchronizeTemplateStyleAttributes())
                .Merge(manager.AcceptChanges())
                .Merge(manager.DeleteStyles())
                .Merge(manager.ApplyStyle())
                .Merge(manager.ReplaceStyles())
                .Merge(manager.Content())
                .Merge(manager.ImportStyles())
                .Merge(manager.LinkTemplate())
                .Merge(manager.SynchronizeScrolling());

        private static IObservable<Unit> SynchronizeScrolling(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenDetailViewCreated().ToDetailView()
		        .SynchronizeScrolling<BusinessObjects.DocumentStyleManager>(style => style.Original, style => style.Content));

        private static IObservable<Unit> SynchronizeTemplateStyleAttributes(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo()
                .Do(_ => {
                    var typesInfo = _.e.TypesInfo;
                    var documentStyle = typesInfo.FindTypeInfo(typeof(DocumentStyle));
                    var memberInfos = documentStyle.OwnMembers;
                    var templateStyle = typesInfo.FindTypeInfo(typeof(TemplateStyle));
                    foreach (var memberInfo in memberInfos)
                    foreach (var memberInfoAttribute in memberInfo.Attributes)
                        templateStyle.FindMember(memberInfo.Name)?.AddAttribute(memberInfoAttribute);
                })
                .ToUnit();

        internal static IObservable<IRichEditDocumentServer> DocumentManagerContentRichEditServer(this IObservable<DetailView> detailView)
            => detailView.SelectMany(view => view.AsDetailView()
                    .WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(styleManager => styleManager.Content)
                    .TakeUntil(view.WhenDisposingView().Select(unit => unit)))
                .Publish().RefCount();

        internal static IObservable<Unit> DocumentStyleManagerDetailView(this ApplicationModulesManager manager,Func<IObservable<DetailView>,IObservable<Unit>> detailView) 
            => manager.WhenApplication(application => application.WhenDetailViewCreated(typeof(BusinessObjects.DocumentStyleManager)).ToDetailView()
		            .SelectMany(view => detailView(view.ReturnObservable())));

        private static IObservable<Unit> FilterReplacementStyles(this IObservable<DetailView> controlsCreated)
            => controlsCreated.SelectMany(view => view.AllStylesListView()
                    .WhenSelectionChanged().Select(_ => (detailView: view, listView: _)))
                .Do(_ => {
                    var viewSelectedObjects = _.listView.SelectedObjects.Cast<IDocumentStyle>().ToArray();
                    var criterion = (CriteriaOperator) CriteriaOperator.CriterionEquals(1, 0);
                    if (viewSelectedObjects.Select(style => style.DocumentStyleType).Distinct().Count() == 1){
                        criterion = CriteriaOperator.Parse($"{nameof(IDocumentStyle.DocumentStyleType)}=?",
                            viewSelectedObjects.First().DocumentStyleType);
                    }

                    _.detailView.ReplacementStylesListView().CollectionSource.Criteria["AllStylesSelection"] = criterion;
                }).ToUnit();

        static IObservable<Unit> DocumentStyleLinkTemplate(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenViewOnFrame(typeof(DocumentStyleLink), ViewType.ListView)
                .Do(frame => {
                    var action = frame.Action("OpenObject");
                    if (action != null) action.Active[nameof(DocumentStyleManager)] = false;
                })
                .ToUnit());

        public static void SynchronizeStyles(this Document document,BusinessObjects.DocumentStyleManager manager,Document defaultPropertiesProvider=null){
	        defaultPropertiesProvider ??= document;
            manager.UnusedStyles.Clear();
            manager.UsedStyles.Clear();
            manager.UnusedStyles.AddRange(document.UnusedStyles(defaultPropertiesProvider:defaultPropertiesProvider));
            manager.UsedStyles.AddRange(document.UsedStyles(defaultPropertiesProvider:defaultPropertiesProvider));
        }

        public static void SynchronizeStyles(this BusinessObjects.DocumentStyleManager manager,Document defaultPropertiesProvider=null){
	        using var richEditDocumentServer = new RichEditDocumentServer();
	        richEditDocumentServer.LoadDocument(manager.Content);
	        richEditDocumentServer.Document.SynchronizeStyles(manager,defaultPropertiesProvider);
	        manager.ObjectSpace.Refresh();
        }
        
        internal static TAction Configure<TAction>(this TAction action) where  TAction:ActionBase{
            action.TargetObjectType = typeof(BusinessObjects.DocumentStyleManager);
            action.Category = StyleManagerActionCategory;
            action.TargetViewType=ViewType.DetailView;
            action.TargetViewNesting=Nesting.Root;
            action.TypeOfView = typeof(DetailView);
            return action;
        }


    }
}