using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraRichEdit;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.DocumentStyleManager{
    public class DeleteTests:CommonTests{
	    [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void DeleteStylesAction_Removes_Styles_and_update_document(){
            using var xafApplication=DocumentStyleManagerModule().Application;
            // application.MockEditorsFactory();
            xafApplication.MockListEditor((view, application, _) =>
                view.Id.Contains(nameof(BusinessObjects.DocumentStyleManager.AllStyles)) ? application.ListEditorMock(view).Object : new GridListEditor(view));
            var server = xafApplication.WhenDetailViewCreated().SelectMany(_ => _.e.View.WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(manager => manager.Content)).Test();
            var tuple = xafApplication.SetDocumentStyleManagerDetailView(Document);

            var documentStyleManager = tuple.documentStyleManager;
            var document = server.Items.First().Document;
            document.NewDocumentStyle(1, DocumentStyleType.Paragraph);
            document.UsedStyles().WhenNotDefaultStyle().Count().ShouldBe(1);
            documentStyleManager.Content = document.ToByteArray(DocumentFormat.OpenXml);
            
            tuple.window.View.AsDetailView().AllStylesListView().Editor.GetMock()
	            .Setup(editor => editor.GetSelectedObjects())
	            .Returns(() => documentStyleManager.AllStyles);
            var deleteStylesAction = tuple.window.Action<DocumentStyleManagerModule>().DeleteStyles();
            var content = documentStyleManager.Content;
            
            deleteStylesAction.DoTheExecute();

            var notDefaultStyle = documentStyleManager.AllStyles.Where(style => style.StyleName.StartsWith(TestExtensions.PsStyleName)).ToArray();
            notDefaultStyle.Length.ShouldBe(0);
            documentStyleManager.Content.ShouldNotBe(content);
        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void Delete_all_unused_styles(){
            using var application=DocumentStyleManagerModule().Application;
            var server = application.WhenDetailViewCreated().SelectMany(_ => _.e.View.WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(manager => manager.Content)).Test();
            var tuple = application.SetDocumentStyleManagerDetailView(Document);

            var documentStyleManager = tuple.documentStyleManager;
            var document = server.Items.First().Document;
            document.NewDocumentStyle(1, DocumentStyleType.Paragraph,true);
            document.UnusedStyles(DocumentStyleType.Paragraph).WhenNotDefaultStyle().Count().ShouldBe(1);
            documentStyleManager.Content = document.ToByteArray(DocumentFormat.OpenXml);
            

            var deleteStylesAction = tuple.window.Action<DocumentStyleManagerModule>().DeleteStyles();
            var content = documentStyleManager.Content;
            
            deleteStylesAction.DoExecute(deleteStylesAction.Items.First(item => item.Id==DeleteService.UnusedItemId));

            var notDefaultStyle = documentStyleManager.AllStyles.Where(style => style.StyleName.StartsWith(TestExtensions.PsStyleName)).ToArray();
            notDefaultStyle.Length.ShouldBe(0);
            documentStyleManager.Content.ShouldNotBe(content);
        }


    }
}