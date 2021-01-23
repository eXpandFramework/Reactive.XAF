using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services{
    internal static class DocumentStyleLinkTemplateService{
        internal static IObservable<Unit> DocumentStyleLinkTemplate(this ApplicationModulesManager manager) =>
            new[]{typeof(ApplyTemplateStyle), typeof(BusinessObjects.DocumentStyleManager)}.ToObservable()
                .SelectMany(type => manager.WhenApplication(application => application.WhenViewOnFrame(type, ViewType.DetailView)
	                .Select(frame => frame.Action("OpenObject")).WhenNotDefault().Cast<SimpleAction>().WhenExecuted()
	                .SelectMany(_ => {
		                var detailView = _.Action.View<DetailView>();
		                var document = (detailView.ObjectTypeInfo.Type == typeof(ApplyTemplateStyle)
			                ? detailView.ApplyTemplateStyleChangedRichEditControl()
			                : detailView.DocumentManagerContentRichEditServer()).Document;
		                return ((DocumentStyleLinkTemplate) _.ShowViewParameters.CreatedView.CurrentObject).DocumentStyleLinks
			                .Do(link => link.SetDefaultPropertiesProvider(document));
	                }).ToUnit().TraceDocumentStyleModule()
                ));

        internal static IObservable<Unit> AssignStyleLinkDocument(this IObservable<DetailView> source) =>
	        source.AssignStyleLinkDocumentWhenDetailViweCreated()
		        .Merge(source.AssingStyleLinkDocumentWhenContentChanged())
		        .Merge(source.AssingStyleLinkDocumentWhenTemplateChanged());

        private static IObservable<Unit> AssingStyleLinkDocumentWhenContentChanged(this IObservable<DetailView> source) => source
            .WhenControlsCreated()
            .SelectMany(view => ((BusinessObjects.DocumentStyleManager) view.CurrentObject).WhenPropertyChanged(_ => _.Content)
                .WhenNotDefault(_ => _.DocumentStyleLinkTemplate)
                .SelectMany(styleManager => {
                    var server = view.DocumentManagerContentRichEditServer();
                    return styleManager.DocumentStyleLinkTemplate.DocumentStyleLinks
                        .Do(link => link.SetDefaultPropertiesProvider(server.Document));
                }))
            .ToUnit().TraceDocumentStyleModule();

        private static IObservable<Unit> AssingStyleLinkDocumentWhenTemplateChanged(this IObservable<DetailView> source) => source
            .WhenControlsCreated()
            .SelectMany(view => ((BusinessObjects.DocumentStyleManager) view.CurrentObject).WhenPropertyChanged(_ => _.DocumentStyleLinkTemplate)
                .WhenNotDefault(styleManager => styleManager.DocumentStyleLinkTemplate)
                .SelectMany(styleManager => {
                    var server = view.DocumentManagerContentRichEditServer();
                    return styleManager.DocumentStyleLinkTemplate.DocumentStyleLinks
                        .Do(link => link.SetDefaultPropertiesProvider(server.Document));
                }))
            .ToUnit().TraceDocumentStyleModule();

        private static IObservable<Unit> AssignStyleLinkDocumentWhenDetailViweCreated(this IObservable<DetailView> source) => source
            .SelectMany(view => view.WhenRichEditDocumentServer(nameof(BusinessObjects.DocumentStyleManager.Content))
                .WhenNotDefault(server => ((BusinessObjects.DocumentStyleManager) view.CurrentObject).DocumentStyleLinkTemplate)
                .SelectMany(server => ((BusinessObjects.DocumentStyleManager) view.CurrentObject).DocumentStyleLinkTemplate.DocumentStyleLinks
                    .Do(link => link.SetDefaultPropertiesProvider(server.Document)))
            )
            .ToUnit().TraceDocumentStyleModule();
        
    }
}