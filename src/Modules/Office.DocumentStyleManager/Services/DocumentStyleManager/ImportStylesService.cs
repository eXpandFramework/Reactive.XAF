using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
	public static class ImportStylesService{
		public static SingleChoiceAction FilterImportStyles(this (DocumentStyleManagerModule, Frame frame) tuple) => tuple.frame.Action(nameof(FilterImportStyles)).As<SingleChoiceAction>();
		public static SimpleAction ImportStyles(this (DocumentStyleManagerModule, Frame frame) tuple) => tuple.frame.Action(nameof(ImportStyles)).As<SimpleAction>();

		internal static IObservable<Unit> ImportStyles(this ApplicationModulesManager manager) => manager.ExecuteActions().Merge(manager.ShowStyleListView());

		private static IObservable<Unit> ShowStyleListView(this ApplicationModulesManager manager) =>
			manager.WhenApplication(application => {
				var viewOnFrame = application.WhenViewOnFrame(typeof(DocumentStyle), ViewType.ListView, Nesting.Root).Publish().RefCount();
				return viewOnFrame.SelectMany(frame => ((NonPersistentObjectSpace)frame.View.ObjectSpace)
					.WhenObjectsGetting().SelectMany(_ => application.DefaultPropertiesProvider(document => {
						var filterImportStyles = frame.Action<DocumentStyleManagerModule>().FilterImportStyles();
						var selectedItemData = (IModelImportStylesItem)filterImportStyles.SelectedItem?.Data;
						_.e.Objects = application.CreateDocumentStyles(document, selectedItemData);
						return Unit.Default.ReturnObservable();
					}))
				)
				.Merge(viewOnFrame.Do(frame => frame.View.ObjectSpace.Refresh()).IgnoreElements().ToUnit());
			});

		private static BindingList<IDocumentStyle> CreateDocumentStyles(this XafApplication xafApplication,
			Document document, IModelImportStylesItem selectedItemData) =>
			xafApplication.GetBytes(selectedItemData)
				.SelectMany(bytes => {
					using var server = new RichEditDocumentServer();
					server.CreateNewDocument();
					server.LoadDocument(bytes);
					return server.Document.AllStyles(defaultPropertiesProvider: document).ToArray();
				})
				.Distinct().ToBindingList();

		private static IEnumerable<byte[]> GetBytes(this XafApplication xafApplication, IModelImportStylesItem selectedItemData) =>
			((IModelOptionsOfficeModule)xafApplication.Model.Options).OfficeModule.ImportStyles
			.Where(item => item == selectedItemData || selectedItemData == null)
			.SelectMany(
				item => {
					using var objectSpace = xafApplication.CreateObjectSpace(item.ModelClass.TypeInfo.Type);
					var criteriaOperator = objectSpace.ParseCriteria(item.Criteria);
					return objectSpace.GetObjects(item.ModelClass.TypeInfo.Type, criteriaOperator)
						.Cast<object>()
						.Select(o => item.Member.MemberInfo.GetValue(o)).Cast<byte[]>().ToArray();
				});

		private static IObservable<Unit> ExecuteActions(this ApplicationModulesManager manager) => manager
			.ShowStylesListView()
			.Merge(manager.FilterImportStyles())
			.Merge(manager.Import());

		private static IObservable<Unit> FilterImportStyles(this ApplicationModulesManager manager) => manager
			.RegisterViewSingleChoiceAction(nameof(FilterImportStyles), action => {
				action.Caption = "Filter";
				action.TargetObjectType = typeof(DocumentStyle);
				action.TargetViewType = ViewType.ListView;
				action.TargetViewNesting = Nesting.Root;
				action.Active[nameof(ImportStylesService)] = false;
				action.Items.Add(new ChoiceActionItem("None", null));
			}, PredefinedCategory.PopupActions)
			.WhenControllerActivated()
			.Do(action => {
				var importStyles = ((IModelOptionsOfficeModule)action.Application.Model.Options).OfficeModule.ImportStyles;
				action.Items.AddRange(importStyles.Select(item => new ChoiceActionItem(item.Caption, item)));
				action.SelectedItem = action.Items.FirstOrDefault(item => item.Data == importStyles.CurrentItem);
			})
			.WhenSelectedItemChanged()
			.Do(action => {
				action.View().AsListView().ObjectSpace.Refresh();
				var importStyles = ((IModelOptionsOfficeModule)action.Application.Model.Options).OfficeModule.ImportStyles;
				importStyles.CurrentItem = (IModelImportStylesItem)action.SelectedItem?.Data;
			})
			.ToUnit();

		private static IObservable<Unit> ShowStylesListView(this ApplicationModulesManager manager) =>
			manager.RegisterViewSimpleAction(nameof(ImportStyles), action => {
				var simpleAction = action.Configure();
				simpleAction.Caption = "Import";
				simpleAction.ImageName = "ImageLoad";
			})
				.WhenActivated()
				.Do(action => action.Active[nameof(ImportStylesService)] =
					((IModelOptionsOfficeModule)action.Application.Model.Options).OfficeModule.ImportStyles.Any())
				.ShowDocumentStyleListView();

		private static IObservable<Unit> Import(this ApplicationModulesManager manager) =>
			manager.WhenApplication(application => application.WhenDetailViewCreated(typeof(Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects.DocumentStyleManager)).ToDetailView()
				.WhenControlsCreated()
				.SelectMany(view => application.WhenViewOnFrame(typeof(DocumentStyle), ViewType.ListView, Nesting.Root)
					.TakeUntil(view.WhenClosed())
					.Import(view.DocumentManagerContentRichEditServer()))
				.ToUnit());

		private static IObservable<DocumentStyle[]> Import(this IObservable<Frame> source, IRichEditDocumentServer server) => source
			.SelectMany(frame => frame.GetController<DialogController>().AcceptAction.WhenExecute()
				.SelectMany(server.Import));

		private static IObservable<DocumentStyle[]> Import(this IRichEditDocumentServer server, SimpleActionExecuteEventArgs args) =>
			args.Action.As<SimpleAction>().WhenExecuteCompleted()
				.Select(_ => _.SelectedObjects.Cast<DocumentStyle>().ToArray())
				.SelectMany(styles => args.Action.Application.DefaultPropertiesProvider(document => {
					server.Document.BeginUpdate();
					foreach (var style in styles)
					{
						server.Document.CreateNewStyle(style, document);
					}
					server.Document.EndUpdate();
					return styles.ReturnObservable();
				}));
		
		private static IObservable<Unit> ShowDocumentStyleListView(this IObservable<SimpleAction> source) =>
			source.WhenExecute().SelectMany(e => {
				var showViewParameters = e.ShowViewParameters;
				var application = e.Action.Application;
				showViewParameters.CreatedView = application.NewView(application.FindListViewId(typeof(DocumentStyle)));
				showViewParameters.TargetWindow = TargetWindow.NewModalWindow;
				var dialogController = new DialogController();
				var conntrollerType = application.Model.ActionDesign.Actions[nameof(FilterImportStyles)].Controller.ControllerType(application.ControllersManager());
				showViewParameters.Controllers.Add(application.CreateController(conntrollerType));
                showViewParameters.Controllers.Add(dialogController);
				return dialogController.WhenActivated()
					.Do(_ => {
						var frame = _.Frame;
						frame.GetController<ListViewProcessCurrentObjectController>().Active[nameof(ImportStylesService)] = false;
						var filterImportStyles = frame.Action<DocumentStyleManagerModule>().FilterImportStyles();
						filterImportStyles.Active[nameof(ImportStylesService)] = true;
					});
			}).ToUnit();
	}
}