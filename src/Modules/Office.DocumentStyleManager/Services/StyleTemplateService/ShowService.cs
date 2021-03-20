using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Templates;
using DevExpress.Persistent.Base;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService{
	public static class ShowService{
		public static SimpleAction ShowApplyStylesTemplate(this (DocumentStyleManagerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ShowApplyStylesTemplate)).As<SimpleAction>();

		internal static IObservable<Unit> ShowStyleTemplate(this ApplicationModulesManager manager){
			var registerAction = manager.RegisterApplyStyleTemplateAction();
			
			return registerAction.Activate()
				.Merge(registerAction.ShowApplyTemplateStyle())
				.ToUnit();
		}

        private static IObservable<Unit> ShowApplyTemplateStyle(this IObservable<SimpleAction> registerAction)
            => registerAction
                .WhenExecute()
                .Do(e => {
                    var application = e.Action.Application;
                    var templateStyle = application.NewApplyStyleTemplate(e.Action.View().AsListView().Model,
                        e.SelectedObjects.Cast<object>().ToArray());
                    var detailView = application.CreateDetailView(templateStyle);
                    e.ShowViewParameters.CreatedView = detailView;
                    e.ShowViewParameters.TargetWindow = TargetWindow.NewModalWindow;
                })
                .ToUnit();

		internal static ApplyTemplateStyle NewApplyStyleTemplate(this XafApplication application, IModelListView listView, params object[] objects){
			var objectSpace = application.CreateObjectSpace(typeof(ApplyTemplateStyle));
			var templateStyle = objectSpace.CreateObject<ApplyTemplateStyle>();
            var defaultMemberMemberInfo = listView.Application.DocumentStyleManager()
				.ApplyTemplateListViews[listView.Id].DefaultMember.MemberInfo;
			var objectsData = objects.GroupBy(o => o.GetType())
				.SelectMany(_ => {
					var space = application.CreateObjectSpace(_.Key);
					return _.Select(o => (name: $"{defaultMemberMemberInfo.GetValue(o)}", key: space.GetKeyValue(o)));
				});
			templateStyle.ListView = listView.Id;
			templateStyle.Documents.AddRange(objectsData.Select(_ => {
				var templateDocument = objectSpace.CreateObject<TemplateDocument>();
				templateDocument.Key = _.key;
				templateDocument.ApplyStyleTemplate = templateStyle;
				templateDocument.Name = _.name;
				return templateDocument;
			}));
			return templateStyle;
		}

        private static IObservable<Unit> Activate(this IObservable<SimpleAction> registerAction)
            => registerAction
                .WhenControllerActivated()
                .Do(action => action.Active[nameof(ShowService)] = action.Application.Model.IsTemplateListView(action.View().Id))
                .WhenActive()
                .TraceDocumentStyleModule(action => action.Id)
                .ToUnit();

		internal static bool IsTemplateListView(this IModelApplication applicationModel, string viewID)
            => applicationModel.DocumentStyleManager().ApplyTemplateListViews.Any(item => item.ListViewId==viewID);

        private static IObservable<SimpleAction> RegisterApplyStyleTemplateAction(this ApplicationModulesManager manager)
            => manager.RegisterViewSimpleAction(nameof(ShowApplyStylesTemplate), action => {
                    action.TargetViewNesting = Nesting.Root;
                    action.TargetViewType = ViewType.ListView;
                    action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                    action.Caption = "Apply Styles";
                    action.ImageName = "XafBarLinkContainerItem";
                    action.PaintStyle = ActionItemPaintStyle.CaptionAndImage;
                }, PredefinedCategory.Tools)
                .Publish().RefCount();
	}
}
