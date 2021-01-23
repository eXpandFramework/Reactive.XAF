using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraRichEdit;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.DocumentStyleManager{
    public class ContentServiceTests:CommonTests{
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void On_Document_Selection_Changed_Select_Used_Style(){
            using var xafApplication=DocumentStyleManagerModule().Application;
            xafApplication.MockListEditor((view, application, collectionSource) => view.Id.Contains(nameof(BusinessObjects.DocumentStyleManager.AllStyles))
                ? application.ListEditorMock(view).Object : new GridListEditor(view));
            var serverObserver = xafApplication.WhenDetailViewCreated().SelectMany(_ => _.e.View.WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(manager => manager.Content)).Test();
            var tuple = xafApplication.SetDocumentStyleManagerDetailView(Document);
            var documentStyleManager = tuple.documentStyleManager;
            var server = serverObserver.Items.First();
            var document = server.Document;
            document.NewDocumentStyle(2, DocumentStyleType.Paragraph);
            documentStyleManager.Content = document.ToByteArray(DocumentFormat.OpenXml);
        
            var style1 = documentStyleManager.UsedStyles.WhenNotDefaultStyle().First();
            var allStylesListEditorMock = tuple.window.View.AsDetailView().AllStylesListView().Editor.GetMock();
            allStylesListEditorMock
	            .SetupSet(editor => editor.FocusedObject=style1).Verifiable();
            server.OnSelectionChanged();
            allStylesListEditorMock.Verify();
        
            document.CaretPosition=document.Paragraphs[1].Range.Start;
            document.DocumentStyleFromPosition().StyleName.ShouldBe($"{TestExtensions.PsStyleName}1");
            var style2 = documentStyleManager.UsedStyles.WhenNotDefaultStyle().Skip(1).First();
            allStylesListEditorMock.SetupSet(editor => editor.FocusedObject=style2).Verifiable();
            server.OnSelectionChanged();
            allStylesListEditorMock.Verify();
        }
    
        [TestCase(2)]
        [TestCase(1)][XpandTest()][Apartment(ApartmentState.STA)]
        public void Replacement_ListView_Displays_Styles_of_same_type_as_the_Selected_Styles(int selectedStyles){
            using var xafApplication=DocumentStyleManagerModule().Application;

            xafApplication.MockListEditor((view, application, collectionSource) => view.Id.Contains(nameof(BusinessObjects.DocumentStyleManager.AllStyles))
                ? application.ListEditorMock(view).Object : new GridListEditor(view));
            
            Document.NewDocumentStyle(3, DocumentStyleType.Paragraph);
            var tuple = xafApplication.SetDocumentStyleManagerDetailView(Document);

            var managerDetailView = tuple.window.View.AsDetailView();
            var allStylesView = managerDetailView.GetListPropertyEditor<BusinessObjects.DocumentStyleManager>(_ => _.AllStyles).Frame.View.AsListView();
            tuple.window.View.AsDetailView().AllStylesListView().Editor.GetMock()
	            .Setup(editor => editor.GetSelectedObjects())
	            .Returns(() => ((BusinessObjects.DocumentStyleManager) managerDetailView.CurrentObject).AllStyles
	            .Where(_ => _.DocumentStyleType == DocumentStyleType.Paragraph).WhenNotDefaultStyle().Skip(1).Take(selectedStyles)
	            .ToArray());
            allStylesView.OnSelectionChanged();

            var replacementStylesView = managerDetailView.GetListPropertyEditor<BusinessObjects.DocumentStyleManager>(_ => _.ReplacementStyles).Frame.View.AsListView();
            var replacementStyles = replacementStylesView.CollectionSource.Objects<DocumentStyle>().ToArray();
            replacementStyles.Length.ShouldBeGreaterThan(0);
            replacementStyles.All(style => style.DocumentStyleType == DocumentStyleType.Paragraph).ShouldBeTrue();
        }

        [Test][Apartment(ApartmentState.STA)]
        public void Synchronize_styles_when_document_modified(){
            
            Await(async () => {
                using var application=DocumentStyleManagerModule().Application;
                var serverObserver = application.WhenDetailViewCreated(typeof(BusinessObjects.DocumentStyleManager)).ToDetailView()
                    .SelectMany(detailView => detailView.AsDetailView().WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(manager => manager.Content)).Test();
            
                var tuple = application.SetDocumentStyleManagerDetailView(Document);
                var richEditDocumentServer = serverObserver.Items.First();
        
                var style = richEditDocumentServer.Document.NewDocumentStyle(1,DocumentStyleType.Paragraph).First().ToDocumentStyle(Document);

                await Observable.While(() => !tuple.documentStyleManager.AllStyles.ToArray().Contains(style) ,
                        Observable.Timer(TimeSpan.FromMilliseconds(200)))
                    .Timeout(TimeSpan.FromSeconds(10)).ToTaskWithoutConfigureAwait();
            
                application.Dispose();

            });            
        }


    }

}