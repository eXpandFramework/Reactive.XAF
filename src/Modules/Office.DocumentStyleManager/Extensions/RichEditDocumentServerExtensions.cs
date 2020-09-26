using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions{
    public static class RichEditDocumentServerExtensions{
        [PublicAPI]
        public static IScheduler EventsScheduler=ImmediateScheduler.Instance;

        public static IObservable<IRichEditDocumentServer> WhenModifiedChanged(this IRichEditDocumentServer server) =>
            Observable.FromEventPattern<EventHandler,EventArgs>(h => server.ModifiedChanged+=h,h => server.ModifiedChanged-=h,EventsScheduler)
                .TransformPattern<EventArgs,IRichEditDocumentServer>()
                .Select(_ => _.sender);

        public static IObservable<IRichEditDocumentServer> WhenContentChanged(this IRichEditDocumentServer server) =>
            Observable.FromEventPattern<EventHandler,EventArgs>(h => server.ContentChanged+=h,h => server.ContentChanged-=h,EventsScheduler)
                .TransformPattern<EventArgs,IRichEditDocumentServer>()
                .Select(_ => _.sender);

        public static IObservable<IRichEditDocumentServer> WhenSelectionChanged(this IRichEditDocumentServer server) =>
            Observable.FromEventPattern<EventHandler,EventArgs>(h => server.SelectionChanged+=h,h => server.SelectionChanged-=h,EventsScheduler)
                .TransformPattern<EventArgs,IRichEditDocumentServer>()
                .Select(_ => _.sender);

        internal static IObservable<IRichEditDocumentServer> WhenRichEditDocumentServer(this DetailView detailView, string member) =>
            detailView.GetPropertyEditor(member).WhenControlCreated().Cast<PropertyEditor>().Select(RichEditControl);

        internal static IRichEditDocumentServer RichEditControl(this PropertyEditor propertyEditor) => 
            (IRichEditDocumentServer) propertyEditor.GetPropertyValue("RichEditControl");

        internal static IObservable<IRichEditDocumentServer> WhenRichEditDocumentServer<T>(this DetailView detailView, Expression<Func<T, object>> memberSelector){
            return detailView.WhenRichEditDocumentServer(memberSelector.MemberExpressionName());
        }

        public static Document LoadDocument(this RichEditDocumentServer richEditDocumentServer, Byte[] bytes){
            using var memoryStream = new MemoryStream(bytes);
            richEditDocumentServer.LoadDocument(memoryStream);
            return richEditDocumentServer.Document;
        }

        public static IObservable<Unit> SynchronizeScrolling<TObject>(this IObservable<DetailView> source,
            Expression<Func<TObject, object>> memberSelector1, Expression<Func<TObject, object>> memberSelector2) =>
            source.When(typeof(TObject)).SelectMany(view => view.WhenRichEditDocumentServer(memberSelector1))
                .SynchronizeScrolling(source.When(typeof(TObject)).SelectMany(view => view.WhenRichEditDocumentServer(memberSelector2)));
        
        public static IObservable<Unit> SynchronizeScrolling(this IObservable<IRichEditDocumentServer> source,IObservable<IRichEditDocumentServer> target) =>
            AppDomain.CurrentDomain.IsHosted()?Observable.Empty<Unit>() : source.Zip(target, (sourceServer, targetServer) => Observable
                    .FromEventPattern(sourceServer.VScrollBar(), "ValueChanged")
                    .Do(_ => targetServer.SetPropertyValue("VerticalScrollValue",
                        sourceServer.GetPropertyValue("VerticalScrollValue")))
                    .ToUnit()
                    .Merge(Observable.FromEventPattern(targetServer.VScrollBar(), "ValueChanged")
                        .Do(_ => sourceServer.SetPropertyValue("VerticalScrollValue",
                            targetServer.GetPropertyValue("VerticalScrollValue")))
                        .ToUnit()))
                .Merge();

        private static object VScrollBar(this IRichEditDocumentServer server){
            return ((IEnumerable) server.GetPropertyValue("Controls")).Cast<object>()
                .First(o => o?.GetType().FullName == "DevExpress.XtraEditors.VScrollBar");
        }
    }

}