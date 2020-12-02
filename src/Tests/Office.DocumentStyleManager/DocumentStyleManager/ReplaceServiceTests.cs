using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.XtraRichEdit;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.DocumentStyleManager{
    public class ReplaceServiceTests:BaseTests{
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void ReplaceStylesAction_SelectionContext_should_be_the_ReplacemenntStyles_ListView(){
            using var application=DocumentStyleManagerModule().Application;
	        var tuple = application.SetDocumentStyleManagerDetailView(Document);

	        tuple.window.Action<DocumentStyleManagerModule>().ReplaceStyles().SelectionContext.ShouldBe(tuple.window.View
		        .AsDetailView().GetListPropertyEditor<BusinessObjects.DocumentStyleManager>(_ => _.ReplacementStyles).Frame.View
		        .AsListView());
        }

        [Test][Apartment(ApartmentState.STA)]
        [XpandTest()]
        public void ReplaceStylesAction_Replace_Styles_and_update_document(){
            using var xafApplication=DocumentStyleManagerModule().Application;
            var server = xafApplication.WhenDetailViewCreated().SelectMany(_ =>
                    _.e.View.WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(manager => manager.Content)).Test();
            var tuple = xafApplication.SetDocumentStyleManagerDetailView(Document);
            var documentStyleManager = tuple.documentStyleManager;
            var document = server.Items.First().Document;
            document.NewDocumentStyle(2, DocumentStyleType.Paragraph);
            documentStyleManager.Content = document.ToByteArray(DocumentFormat.OpenXml);
            tuple.window.View.AsDetailView().AllStylesListView().Editor.GetMock()
	            .Setup(editor => editor.GetSelectedObjects())
	            .Returns(() => documentStyleManager.AllStyles.Where(style => style.DocumentStyleType == DocumentStyleType.Paragraph).ToArray());
            var replacementStyle = documentStyleManager.ReplacementStyles.WhenNotDefaultStyle().Skip(1).Take(1).First();
            tuple.window.View.AsDetailView().ReplacementStylesListView().Editor.GetMock()
	            .Setup(editor => editor.GetSelectedObjects())
	            .Returns(() => new object[]{replacementStyle});
            var replaceStylesAction = tuple.window.Action<DocumentStyleManagerModule>().ReplaceStyles();

            var content = documentStyleManager.Content;
            var byteArray = document.ToByteArray(DocumentFormat.OpenXml);
            replaceStylesAction.DoTheExecute(true);

            var notDefaultStyle = documentStyleManager.AllStyles.WhenNotDefaultStyle().ToArray();
            notDefaultStyle.Length.ShouldBeGreaterThan(0);
            replacementStyle.Used.ShouldBeTrue();
            var retainUndo = documentStyleManager.Content==content;
            retainUndo.ShouldBeTrue();
            document.ToByteArray(DocumentFormat.OpenXml).ShouldNotBe(byteArray);
        }

    }
}