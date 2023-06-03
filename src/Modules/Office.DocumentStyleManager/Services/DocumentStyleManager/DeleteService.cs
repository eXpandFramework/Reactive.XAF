using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.XtraRichEdit;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
    public static class DeleteService{
        public const string SelectedItemId = "Selected";
        public const string UnusedItemId = "Unused";
        public static SingleChoiceAction DeleteStyles(this (DocumentStyleManagerModule, Frame frame) tuple) => tuple.frame.Action(nameof(DeleteStyles)).As<SingleChoiceAction>();

        static IObservable<Unit> DeleteStyles(this IObservable<SingleChoiceAction> source) => source
            .WhenExecute().SelectMany(_ => {
                var view = _.Action.View<DetailView>();
                var server = view.DocumentManagerContentRichEditServer();
                var allStylesListView = view.AllStylesListView();
                var styles = allStylesListView.SelectedObjects.Cast<IDocumentStyle>().ToArray();
                if (_.Action.As<SingleChoiceAction>().SelectedItem.Id == "Unused"){
                    styles = allStylesListView.CollectionSource.Objects<IDocumentStyle>()
                        .Where(style => !style.Used && !style.IsDefault).ToArray();
                }

                server.Document.DeleteStyles(styles);
                var documentStyleManager = ((BusinessObjects.DocumentStyleManager) view.CurrentObject);
                documentStyleManager.Content = server.Document.ToByteArray(DocumentFormat.OpenXml);
                return view.Application().DefaultPropertiesProvider(document => {
		                documentStyleManager.SynchronizeStyles(document);
		                return Unit.Default.Observe();
	                })
                    .TraceDocumentStyleModule(_ => styles.Select(style => style.StyleName).Join(","));
            });

        internal static IObservable<Unit> DeleteStyles(this ApplicationModulesManager manager) =>
            manager.RegisterAction().DeleteStyles();

        private static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager) =>
            manager
                .RegisterViewSingleChoiceAction(nameof(DeleteStyles), action => {
                    action.Configure();
                    action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                    action.DefaultItemMode=DefaultItemMode.FirstActiveItem;
                    action.Items.Add(new ChoiceActionItem(SelectedItemId,"Selected", null));
                    action.Items.Add(new ChoiceActionItem(UnusedItemId,"Unused", null));
                });



    }
}