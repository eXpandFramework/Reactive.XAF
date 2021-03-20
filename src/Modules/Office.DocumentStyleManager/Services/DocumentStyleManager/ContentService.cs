using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
    public static class ContentService{
        internal static IObservable<Unit> Content(this ApplicationModulesManager manager) =>
	        manager.DocumentStyleManagerDetailView(detailView => {
		        var contentRichEditServer = detailView.DocumentManagerContentRichEditServer();
		        return detailView.SelectActiveStyle( contentRichEditServer)
			        .Merge(detailView.WhenControlsCreated().SynchronizeStylesWhenContextModified(contentRichEditServer))
			        .Merge(detailView.SynchronizeEditorPositions( contentRichEditServer));
	        });

        private static IObservable<Unit> SelectActiveStyle(this IObservable<DetailView> source,IObservable<IRichEditDocumentServer> contentRichEditServer) 
            => contentRichEditServer.Select(server => server)
                .Zip(source.Select(view => view))
                .SelectMany(_ => _.First.WhenSelectionChanged().Select(server => _))
                .SelectMany(t => t.Second.Application().DefaultPropertiesProvider(defaultPropertiesProvider => {
		                var server = t.First;
		                var documentStyleManager = (BusinessObjects.DocumentStyleManager) t.Second.CurrentObject;
		                var document = server.Document;
		                documentStyleManager.Position = document.CaretPosition.ToInt();
		                documentStyleManager.Paragraph = document.ParagraphFromPosition() + 1;
                    
		                var documentStyle = document.DocumentStyleFromPosition(defaultPropertiesProvider);
		                if (documentStyle != null){
			                var allStylesListView = t.Second.AllStylesListView();
			                var currentObject = allStylesListView.CollectionSource.Objects<DocumentStyle>()
				                .FirstOrDefault(style => style.Equals(documentStyle));
			                allStylesListView.CurrentObject = currentObject;
		                }

		                return Unit.Default.ReturnObservable().TraceDocumentStyleModule(_ => documentStyle?.StyleName??nameof(SelectActiveStyle));
                }));

        private static IObservable<Unit> SynchronizeEditorPositions(this IObservable<DetailView> source, IObservable<IRichEditDocumentServer> contentRichEditServer){
            var originalRichEditServer = source.SelectMany(detailView => detailView.WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(m => m.Original));
            return contentRichEditServer.Zip(originalRichEditServer, (contentServer, originalServer) => (contentServer, originalServer))
                .SelectMany(_ => _.contentServer.WhenSelectionChanged()
                    .Do(server => _.originalServer.Document.CaretPosition = _.originalServer.Document.CreatePosition(server.Document.CaretPosition.ToInt())))
                .ToUnit().TraceDocumentStyleModule();
        }

        private static IObservable<Unit> SynchronizeStylesWhenContextModified(this IObservable<DetailView> detailView, IObservable<IRichEditDocumentServer> contentRichEditServer){
	        var documentStyleManagerModified = detailView
                .SelectMany(view => view.ObjectSpace.WhenModifiedObjects<BusinessObjects.DocumentStyleManager>(m => m.Content)
	                .SelectMany(manager => view.Application().DefaultPropertiesProvider(document => {
			                manager.SynchronizeStyles(document);
			                return Unit.Default.ReturnObservable();
		                })))
                
	                .ToUnit();
            
            var documentContentChanged = detailView
	            .Zip(contentRichEditServer)
                .SelectMany(_ => _.Second.WhenContentChanged()
                    .Throttle(TimeSpan.FromSeconds(1))
                    .ObserveOn(SynchronizationContext.Current)
                    .SelectMany(server => _.First.Application().DefaultPropertiesProvider(document => server.ResetStyleCollections((_.First,document))
		                .Do(documentServer => documentServer.SelectActiveStyle((_.First,document))))))
                .ToUnit();
            return documentStyleManagerModified.Merge(documentContentChanged).TraceDocumentStyleModule();
        }

        private static void SelectActiveStyle(this IRichEditDocumentServer server, (DetailView view, Document defaultPropertiesProvider) detailView){
            var document = server.Document;
            var documentStyleManager = (BusinessObjects.DocumentStyleManager)detailView.view.CurrentObject;
            documentStyleManager.Position = document.CaretPosition.ToInt();
            documentStyleManager.Paragraph = document.ParagraphFromPosition() + 1; 
            var documentStyle = document.DocumentStyleFromPosition(detailView.defaultPropertiesProvider);
            if (documentStyle != null){
                var allStylesListView = detailView.view.AllStylesListView();
                allStylesListView.CurrentObject = allStylesListView.CollectionSource.Objects<DocumentStyle>().First(style => style.Equals(documentStyle));
            }
        }
        static IObservable<GenericEventArgs<IEnumerable<object>>> CustomizeDatasource(
            this IRichEditDocumentServer server, ListView listView,  (DetailView view, Document defaultPropertiesProvider) detailView,IEnumerable<DocumentStyle> styles=null){
	        var customizeDatasource = ((NonPersistentPropertyCollectionSource) listView.CollectionSource).Datasource
		        .Do(e => {
			        e.Handled = true;
			        e.Instance =new BindingList<DocumentStyle>(ImmutableList.CreateRange(styles!));
		        })
		        .FirstAsync();
	        return (styles == null
                ? listView.Application()
                    .DefaultPropertiesProvider(document => {
                        var usedStyles = server.Document.UsedStyles(defaultPropertiesProvider: document).ToArray();
                        var unUsedStyles = usedStyles.ToArray();
                        styles = unUsedStyles
                            .Concat(server.Document.UnusedStyles(
                                defaultPropertiesProvider: detailView.defaultPropertiesProvider))
                            .Cast<DocumentStyle>().Distinct().Where(_ => !_.IsDeleted).ToArray();
                        var documentStyleManager = (BusinessObjects.DocumentStyleManager) detailView.view.CurrentObject;
                        documentStyleManager.UsedStyles.Clear();
                        documentStyleManager.UsedStyles.AddRange(usedStyles);
                        documentStyleManager.UnusedStyles.Clear();
                        documentStyleManager.UnusedStyles.AddRange(unUsedStyles);
                        return customizeDatasource;
                    })
                : customizeDatasource).TraceDocumentStyleModule();
        }

        private static IObservable<IRichEditDocumentServer> ResetStyleCollections(this IRichEditDocumentServer server, (DetailView view, Document defaultPropertiesProvider) detailView){
            if (detailView.view.IsDisposed){
                return Observable.Empty<IRichEditDocumentServer>();
            }
            var allStylesListView = detailView.view.AllStylesListView();
            return Observable.Create<GenericEventArgs<IEnumerable<object>>>(observer => {
                    var publish = server.CustomizeDatasource(allStylesListView,detailView).Publish();
                    var disposable = new CompositeDisposable(publish.Connect(), publish.Subscribe(observer));
                    allStylesListView.CollectionSource.ResetCollection();
                    return disposable;
                }).ToUnit()
                .Concat(Observable.Create<GenericEventArgs<IEnumerable<object>>>(observer => {
                    var documentStyles = allStylesListView.CollectionSource.Objects<DocumentStyle>().ToArray();
                    var replacementStylesListView = detailView.view.ReplacementStylesListView();
                    var publish = server.CustomizeDatasource(replacementStylesListView, detailView,documentStyles).Publish();
                    var disposable = new CompositeDisposable(publish.Connect(), publish.Subscribe(observer));
                    replacementStylesListView.CollectionSource.ResetCollection();
                    return disposable;
                }).ToUnit())
                .To(server)
                .TraceDocumentStyleModule();
        }

    }
}