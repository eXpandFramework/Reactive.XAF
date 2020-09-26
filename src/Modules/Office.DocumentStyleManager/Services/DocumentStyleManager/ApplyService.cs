using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
    public static class ApplyService{
        
        public static SimpleAction ApplyStyle(this (DocumentStyleManagerModule, Frame frame) tuple) => tuple.frame.Action(nameof(ApplyStyle)).As<SimpleAction>();

        internal static IObservable<Unit> ApplyStyle(this ApplicationModulesManager manager){
            var registerAction = manager.RegisterAction().Publish().RefCount();
            return registerAction.ApplyStyle()
                .Merge(registerAction.ApplyStylesWhenListViewProcessCurrentObject( ));
        }

        private static IObservable<Unit> ApplyStylesWhenListViewProcessCurrentObject(this IObservable<SimpleAction> source) => source
            .WhenActivated()
            .SelectMany(action => action.View<DetailView>().WhenControlsCreated()
                .Select(view => view.GetListPropertyEditor<Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects.DocumentStyleManager>(manager => manager.AllStyles).Frame)
                .SelectMany(frame => frame.GetController<ListViewProcessCurrentObjectController>().ProcessCurrentObjectAction
                    .WhenExecuting()
                    .Do(_ => {
                        _.e.Cancel = true;
                        action.DoExecute();
                    })))
            .ToUnit();

        private static IObservable<SimpleAction> RegisterAction(this ApplicationModulesManager manager) => manager
                .RegisterViewSimpleAction(nameof(ApplyStyle), action => action.Configure());
                
        private static IObservable<Unit> ApplyStyle(this IObservable<SimpleAction> source) =>
            source.WhenExecute()
	            .SelectMany(e => e.Action.Application.DefaultPropertiesProvider(document => {
		            var view = e.Action.View<DetailView>();
		            var server = view.DocumentManagerContentRichEditServer();
		            var styles = view.AllStylesListView().SelectedObjects.Cast<IDocumentStyle>().ToArray();
		            server.Document.ApplyStyle(styles.First(), document);
		            return Unit.Default.ReturnObservable();
	            }))
	            .ToUnit();
    }
}