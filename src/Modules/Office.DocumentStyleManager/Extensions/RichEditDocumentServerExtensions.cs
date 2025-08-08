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

using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions{
    public static class RichEditDocumentServerExtensions{
        
        public static IScheduler EventsScheduler=ImmediateScheduler.Instance;

        public static IObservable<IRichEditDocumentServer> WhenModifiedChanged(this IRichEditDocumentServer server) 
            => server.ProcessEvent(nameof(IRichEditDocumentServer.ModifiedChanged)).To(server).TakeUntilDisposed();

        public static IObservable<IRichEditDocumentServer> TakeUntilDisposed(this IObservable<IRichEditDocumentServer> source)
            => source.TakeWhileInclusive(server => !server.IsDisposed);
        
        public static IObservable<IRichEditDocumentServer> WhenContentChanged(this IRichEditDocumentServer server) 
            => server.ProcessEvent(nameof(IRichEditDocumentServer.ContentChanged)).To(server).TakeUntilDisposed();

        public static IObservable<IRichEditDocumentServer> WhenSelectionChanged(this IRichEditDocumentServer server) 
            => server.ProcessEvent(nameof(IRichEditDocumentServer.SelectionChanged)).To(server).TakeUntilDisposed();

        internal static IObservable<IRichEditDocumentServer> WhenRichEditDocumentServer(this DetailView detailView, string member) 
            => detailView.GetPropertyEditor(member).WhenControlCreated().Cast<PropertyEditor>().Select(RichEditControl);

        internal static IRichEditDocumentServer RichEditControl(this PropertyEditor propertyEditor) 
            => (IRichEditDocumentServer) propertyEditor.GetPropertyValue("RichEditControl");

        internal static IObservable<IRichEditDocumentServer> WhenRichEditDocumentServer<T>(this DetailView detailView, Expression<Func<T, object>> memberSelector) 
            => detailView.WhenRichEditDocumentServer(memberSelector.MemberExpressionName());

        public static Document LoadDocument(this RichEditDocumentServer richEditDocumentServer, Byte[] bytes){
            using var memoryStream = new MemoryStream(bytes);
            richEditDocumentServer.LoadDocument(memoryStream);
            return richEditDocumentServer.Document;
        }

        public static IObservable<Unit> SynchronizeScrolling<TObject>(this IObservable<DetailView> source,
            Expression<Func<TObject, object>> memberSelector1, Expression<Func<TObject, object>> memberSelector2) 
            => source.When(typeof(TObject)).SelectMany(view => view.WhenRichEditDocumentServer(memberSelector1))
                .SynchronizeScrolling(source.When(typeof(TObject)).SelectMany(view => view.WhenRichEditDocumentServer(memberSelector2)));
        
        public static IObservable<Unit> SynchronizeScrolling(this IObservable<IRichEditDocumentServer> source,IObservable<IRichEditDocumentServer> target) 
            => AppDomain.CurrentDomain.IsHosted()?Observable.Empty<Unit>() : source.Zip(target, (sourceServer, targetServer) => sourceServer.VScrollBar().ProcessEvent("ValueChanged")
                    .Do(_ => targetServer.SetPropertyValue("VerticalScrollValue", sourceServer.GetPropertyValue("VerticalScrollValue"))).ToUnit()
                    .Merge(targetServer.VScrollBar().ProcessEvent("ValueChanged")
                        .Do(_ => sourceServer.SetPropertyValue("VerticalScrollValue", targetServer.GetPropertyValue("VerticalScrollValue"))).ToUnit())).Merge();

        private static object VScrollBar(this IRichEditDocumentServer server){
            return ((IEnumerable) server.GetPropertyValue("Controls")).Cast<object>()
                .First(o => o?.GetType().FullName == "DevExpress.XtraEditors.VScrollBar");
        }
    }

}