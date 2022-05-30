// using System;
// using System.Drawing;
// using System.IO;
// using System.Linq;
// using System.Reactive;
// using System.Reactive.Linq;
// using System.Windows.Forms;
// using System.Xml.Serialization;
// using DevExpress.ExpressApp;
// using DevExpress.ExpressApp.Model;
// using DevExpress.ExpressApp.Win;
// using DevExpress.ExpressApp.Win.Templates;
// using DevExpress.ExpressApp.Win.Utils;
// using DevExpress.XtraBars.Docking2010;
// using DevExpress.XtraBars.Docking2010.Views;
// using DevExpress.XtraBars.Docking2010.Views.Tabbed;
// using Fasterflect;
// using Xpand.Extensions.LinqExtensions;
// using Xpand.Extensions.ObjectExtensions;
// using Xpand.Extensions.Reactive.Transform;
// using Xpand.Extensions.XAF.ModelExtensions;
// using Xpand.XAF.Modules.Reactive.Services;
// using View = DevExpress.ExpressApp.View;
//
// namespace Xpand.XAF.Modules.Windows {
// 	public interface IModelMdiLayoutStorage:IModelNode {
// 		IModelMdiLayoutStorageItems LayoutStorageItems { get; }
// 	}
// 	public interface IModelMdiLayoutStorageItems:IModelNode,IModelList<IModelMdiLayoutStorageItem> {
// 		
// 	}
//
// 	public interface IModelMdiLayoutStorageItem : IModelNode {
// 		Rectangle Bounds { get; set; }
// 		FormWindowState WindowState { get; set; }
// 	}
//
// 	public static class DocumentManagerService{
// 		public static IObservable<Unit> ConnectDocumentManager(this ApplicationModulesManager manager){
//
// 			return manager.WhenApplication(application 
// 					=> application.SaveFloatWindow().Merge(application.RestoreFloatWindow()))
//                 
// 				// .SelectMany(documentManager => documentManager.DockManager.reg)
// 				// .Do(documentManager => documentManager.View.FloatingDocumentContainer = FloatingDocumentContainer.SingleDocument)
// 				.ToUnit()
// 				.Merge(manager.WhenExtendingModel().Do(extenders => extenders.Add(typeof(IModelOptions),typeof(IModelMdiLayoutStorage))).ToUnit())
// 				;
// 		}
//
// 		private static IObservable<Unit> RestoreFloatWindow(this XafApplication application) 
// 			=> application.WhenEvent(nameof(XafApplication.ShowViewStrategyChanged))
// 				.SelectMany(xafApplication => application.ShowViewStrategy.WhenEvent(nameof(MdiShowViewStrategy.StartupWindowLoad))
// 				// .RestoreLayout(application)
// 				.Select(pattern => ((IXafDocumentsHostWindow)application.MainWindow.Template).DocumentManager.View)
// 				.SelectMany(args => ((IModelMdiLayoutStorage)application.Model.Options).LayoutStorageItems
// 					.SelectMany(item => args.Documents.ToArray())
// 						.Do(document => args.AddFloatingDocumentsHost(document)))).ToUnit();
//
// 		private static IObservable<CustomRestoreViewLayoutEventArgs> RestoreLayout(this IObservable<CustomRestoreViewLayoutEventArgs> source,XafApplication application) 
// 			=> source.Do(args => {
// 				var restorer = new DocumentManagerStateRestorer();
// 				DocumentManagerState documentManagerState = null;
// 				using (var stream =
// 				       new MemoryStream(
// 					       Convert.FromBase64String(((IModelOptionsTabbedMdiLayout)application.Model.Options).DocumentManagerState))) {
// 					try {
// 						documentManagerState =
// 							(DocumentManagerState)new XmlSerializer(typeof(DocumentManagerState)).Deserialize(stream);
// 					}
// 					catch (Exception ex) {
// 						DevExpress.Persistent.Base.Tracing.Tracer.LogError(ex);
// 					}
// 				}
//
// 				if (documentManagerState != null) {
// 					if (application.Model != null) {
// 						restorer.ApplyShowTabImage(ModelOptionsHelper.GetShowTabImageValue(application.Model));
// 					}
//
// 					restorer.DeserializeDocumentManagerState(args.View, documentManagerState,
// 						application.CreateDocumentControlDelegate(restorer),
// 						description => (bool)application.ShowViewStrategy.CallMethod("CanProcessDocumentControlDescriptionDefault", description));
// 				}
//
// 			});
//
// 		private static CreateDocumentControlDelegate CreateDocumentControlDelegate(this XafApplication application, DocumentManagerStateRestorer restorer) 
// 			=> description => {
// 				var shortcut = ViewShortcut.FromString(description.SerializedControl);
// 				if(!shortcut.IsEmpty) {
// 					var view = application.ProcessShortcut(shortcut);
// 					if(view != null) {
// 						return ((WinWindow)application.ShowViewStrategy.CallMethod("CreateWindow",new ShowViewParameters(view), new ShowViewSource(
// 							((MdiShowViewStrategy)application.ShowViewStrategy).Explorers[0], null), false)).Form;
// 					} 
// 				}
// 				return null;
//
// 			};
//
// 		// private static IObservable<Unit> SaveFloatWindow(this XafApplication application) 
// 		// 	=> application.WhenHostWindowRegistered().SelectMany(hostWindow => application.ShowViewStrategy
// 		// 		.WhenEvent<CustomSaveViewLayoutEventArgs>(nameof(MdiShowViewStrategy.CustomSaveViewLayout))
// 		// 		.Do(e => {
// 		// 			var items = ((IModelMdiLayoutStorage)application.Model.Options).LayoutStorageItems;
// 		// 			items.ClearNodes();
// 		// 			var baseDocumentCollection = e.View.Documents;
// 		// 			var item = items.AddNode<IModelMdiLayoutStorageItem>(e.View.Id());
// 		// 			item.Bounds = hostWindow.Bounds;
// 		// 			item.WindowState = hostWindow.WindowState;
// 		// 			
// 		//
// 		// 		})).ToUnit();
// 		private static IObservable<Unit> SaveFloatWindow(this XafApplication application) 
// 			=> application.WhenHostWindowUnRegistered()
// 				.SelectMany(t => t.View.Documents.Do(document => {
// 					var items = ((IModelMdiLayoutStorage)application.Model.Options).LayoutStorageItems;
// 					items[document.ControlName]?.Remove();
// 				}))
// 				.ToUnit()
// 				.Merge(application.WhenHostWindowRegistered()
// 					.SelectMany(t => t.View.Documents.Do(document => {
// 						var items = ((IModelMdiLayoutStorage)application.Model.Options).LayoutStorageItems;
// 						var item =items[document.ControlName]?? items.AddNode<IModelMdiLayoutStorageItem>(document.ControlName);
// 						item.Bounds = t.HostWindow.As<Form>().Bounds;
// 						item.WindowState = t.HostWindow.As<Form>().WindowState;
// 					}))
// 					.ToUnit());
//
// 		private static string Id(this BaseView baseView) 
// 			=> baseView.Documents.Select(document => document.ControlName).Join(",");
//
// 		
// 		private static IObservable<CustomRestoreViewLayoutEventArgs> WhenCustomRestoreViewLayout(this XafApplication application) 
// 			=> application.WhenFrameCreated(TemplateContext.ApplicationWindow)
// 				.SelectMany(_ => application.ShowViewStrategy.WhenEvent<CustomRestoreViewLayoutEventArgs>(nameof(MdiShowViewStrategy.CustomRestoreViewLayout)))
// 				.Do(e => e.Handled=true);
//
//
// 		// private static void SaveLayout(this Frame frame) {
// 		// 	var documentManagerView = ((IXafDocumentsHostWindow)frame.Template).DocumentManager.View;
// 		// 	var stateRestorer = new DocumentManagerStateRestorer();
// 		// 	var state = stateRestorer.SerializeDocumentManagerState(documentManagerView,
// 		// 		control => {
// 		// 			var window = (WinWindow)stateRestorer.CallMethod("FindWindowByForm", control as Form);
// 		// 			return (bool)stateRestorer.CallMethod("CanCreateViewDescription", control, window)
// 		// 				? new DocumentControlDescription(window.View.CreateShortcut().ToString(), window.View.Model.ImageName)
// 		// 				: null;
// 		// 		});
// 		// 	using MemoryStream stream = new MemoryStream();
// 		// 	new XmlSerializer(typeof(DocumentManagerState)).Serialize(stream, state);
// 		// 	((IModelOptionsTabbedMdiLayout)frame.Application.Model).DocumentManagerState = Convert.ToBase64String(stream.ToArray());
// 		// }
//
//
// 		private static IObservable<(TabbedView View, IDocumentsHostWindow HostWindow)> WhenHostWindowUnRegistered(this XafApplication application) 
// 			=>application.WhenDocumentManagerViewChanged()
// 				.SelectMany(tabbedView => tabbedView.WhenEvent<DocumentsHostWindowEventArgs>(nameof(TabbedView.UnregisterDocumentsHostWindow))
// 					.Select(eventArgs => (tabbedView,eventArgs.HostWindow)));
//
// 		private static IObservable<(TabbedView View, IDocumentsHostWindow HostWindow)> WhenHostWindowRegistered(this XafApplication application) 
// 			=> application.WhenDocumentManagerViewChanged()
// 				.SelectMany(tabbedView => tabbedView.WhenEvent<DocumentsHostWindowEventArgs>(nameof(TabbedView.RegisterDocumentsHostWindow))
// 					.Select(eventArgs => (tabbedView,eventArgs.HostWindow)));
//
// 		private static IObservable<TabbedView> WhenDocumentManagerViewChanged(this XafApplication application) 
// 			=> application.WhenFrameCreated()
// 				.Where(frame => frame.Template is IXafDocumentsHostWindow)
// 				.Select(frame => frame.Template).Cast<IXafDocumentsHostWindow>()
// 				.Select(window => window.DocumentManager)
// 				.SelectMany(documentManager => documentManager.WhenEvent<ViewEventArgs>(nameof(DocumentManager.ViewChanged))
// 					.Select(args => args.View)).Cast<TabbedView>();
// 	}
// }