using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
    public static class ReplaceStylesService{
	    public static SimpleAction ReplaceStyles(this (DocumentStyleManagerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ReplaceStyles)).As<SimpleAction>();

        internal static IObservable<Unit> ReplaceStyles(this ApplicationModulesManager manager){
	        var registerViewSimpleAction = manager.RegisterViewSimpleAction();
	        return registerViewSimpleAction.ReplaceStyles()
		        .Merge(registerViewSimpleAction.ConfigureSelectionContext());
        }

        private static IObservable<SimpleAction> RegisterViewSimpleAction(this ApplicationModulesManager manager) 
            => manager.RegisterViewSimpleAction(nameof(ReplaceStyles), action => {
		        var simpleAction = action.Configure();
		        simpleAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
	        }).Publish().RefCount();

        internal static IObservable<Unit> ConfigureSelectionContext(this IObservable<ActionBase> source)
            => source.WhenChanged(ActionChangedType.SelectionContext)
		        .Do(action => {
                    var replacementStylesListView = action.View<DetailView>().ReplacementStylesListView();
                    action.SelectionContext = replacementStylesListView;
                })
		        .ToUnit();

        private static IObservable<Unit> ReplaceStyles(this IObservable<SimpleAction> source) 
            => source.WhenExecute()
                .SelectMany(_ => _.Action.Application.DefaultPropertiesProvider(document => {
	                var view = _.Action.View<DetailView>();
	                var replacementStyles = view.ReplacementStylesListView().SelectedObjects.Cast<IDocumentStyle>().ToArray();
	                var styles = view.AllStylesListView().SelectedObjects.Cast<IDocumentStyle>().ToArray();
	                view.DocumentManagerContentRichEditServer().Document.ReplaceStyles(replacementStyles.First(),document,styles);
	                return Observable.Empty<Unit>()
                        .TraceDocumentStyleModule(_ => styles.Select(style => style.StyleName).Join(", "));
                }));

    }
}