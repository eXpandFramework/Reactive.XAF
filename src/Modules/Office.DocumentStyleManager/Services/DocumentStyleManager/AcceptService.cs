using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.XtraRichEdit;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
    public static class AcceptService{
        public static SimpleAction CancelStyleManagerChanges(this (DocumentStyleManagerModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(CancelStyleManagerChanges)).As<SimpleAction>();

        public static SimpleAction AcceptStyleManagerChanges(this (DocumentStyleManagerModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(AcceptChanges)).As<SimpleAction>();

        internal static IObservable<Unit> AcceptChanges(this ApplicationModulesManager manager) =>
            manager.RegisterViewSimpleAction(nameof(CancelStyleManagerChanges),
                    action => {
	                    var simpleAction = action.Configure();
	                    simpleAction.Caption = "Cancel";
	                    simpleAction.ImageName = "Action_Cancel";
                    }).CancelChanges()
                .Merge(manager.RegisterViewSimpleAction(nameof(AcceptChanges),
                    action => {
	                    var simpleAction = action.Configure();
	                    simpleAction.Caption = "Accept";
	                    simpleAction.ImageName = "Action_Grant";
                    }).AcceptChanges());

        private static IObservable<Unit> CancelChanges(this IObservable<SimpleAction> source) =>
            source.WhenExecute().Do(args => args.Action.View().Close()).ToUnit();
        private static IObservable<Unit> AcceptChanges(this IObservable<SimpleAction> source) 
            => source.WhenExecute()
                .SelectMany(_ => {
	                var frame = _.Action.Controller.Frame;
                    frame.UpdateCallerObject();
                    frame.SaveDocumentStyleLinkTemplate();
                    frame.View.Close();
                    return Observable.Empty<Unit>();
                });

        private static void SaveDocumentStyleLinkTemplate(this Frame frame){
            var template = ((IObjectSpaceLink) ((Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects.DocumentStyleManager) frame.View.CurrentObject).DocumentStyleLinkTemplate);
            template?.ObjectSpace.CommitChanges();
        }

        private static void UpdateCallerObject(this Frame frame){
	        var infoController = frame.GetController<ContentInfoController>();
	        var server = frame.View.AsDetailView().DocumentManagerContentRichEditServer();
	        var byteArray = server.Document.ToByteArray(DocumentFormat.OpenXml);
	        ((Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects.DocumentStyleManager) frame.View.CurrentObject).Content = byteArray;
	        infoController.MemberInfo.SetValue(infoController.CallerObject, byteArray);
        }
    }
}