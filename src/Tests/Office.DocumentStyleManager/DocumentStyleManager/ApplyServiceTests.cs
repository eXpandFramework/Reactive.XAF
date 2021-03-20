using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Win.Editors;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.DocumentStyleManager{
    public class ApplyServiceTests:CommonTests{
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void Apply_Style_to_Document(){
	        using var application=DocumentStyleManagerModule().Application;
            MockAllStylesListEditor(application);
            var documentStyle = Document.NewDocumentStyle(1,DocumentStyleType.Paragraph,true).First().ToDocumentStyle(Document);
            var server = application.WhenDetailViewCreated().SelectMany(_ => _.e.View.WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(manager => manager.Content)).Test();
            var tuple = application.SetDocumentStyleManagerDetailView(Document);
            tuple.window.View.AsDetailView().AllStylesListView().Editor.GetMock()
	            .Setup(editor => editor.GetSelectedObjects())
	            .Returns(() => new List<IDocumentStyle>(){documentStyle});
            var content = tuple.documentStyleManager.Content;
            tuple.window.Action<DocumentStyleManagerModule>().ApplyStyle().DoTheExecute();
            
            server.Items.First().Document.Paragraphs[0].Style.ToDocumentStyle(Document).ShouldBe(documentStyle);
            var retainUndo = tuple.documentStyleManager.Content==content;
            retainUndo.ShouldBeTrue();
        }

        private void MockAllStylesListEditor(XafApplication xafApplication){
	        // xafApplication.MockEditorsFactory();
	        xafApplication.MockListEditor((view, application, _) =>
		        view.Id.Contains(nameof(BusinessObjects.DocumentStyleManager.AllStyles))
			        ? application.ListEditorMock(view).Object
			        : new GridListEditor(view));
        }

        [Test][Apartment(ApartmentState.STA)][XpandTest()]
        public void Executes_When_AllStyles_ListViewShowObjectAction_executes(){
	        using var application=DocumentStyleManagerModule().Application;
	        MockAllStylesListEditor(application);
            var documentStyle = Document.NewDocumentStyle(1,DocumentStyleType.Paragraph).First().ToDocumentStyle(Document);
            var tuple = application.SetDocumentStyleManagerDetailView(Document);
            tuple.window.View.AsDetailView().AllStylesListView().Editor.GetMock()
	            .Setup(editor => editor.GetSelectedObjects())
	            .Returns(() => new List<IDocumentStyle>(){documentStyle});
            var applyActionExecuteObserver = tuple.window.Action<DocumentStyleManagerModule>().ApplyStyle().WhenExecute().Test();
            
            var processCurrentObjectAction = tuple.window.View.AsDetailView()
                .GetListPropertyEditor<BusinessObjects.DocumentStyleManager>(_ => _.AllStyles).Frame
                .GetController<ListViewProcessCurrentObjectController>().ProcessCurrentObjectAction;
            
            processCurrentObjectAction.DoTheExecute(true);
            
            applyActionExecuteObserver.Items.Count.ShouldBe(1);
        }
    }
}