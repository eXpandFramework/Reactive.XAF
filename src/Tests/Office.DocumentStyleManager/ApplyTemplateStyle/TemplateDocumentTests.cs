using System.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.ApplyTemplateStyle{
	public class TemplateDocumentTests : BaseTests{
		[Test][XpandTest()][Apartment(ApartmentState.STA)]
		public void When_Selected_TemplateDocument_Changed_Display_Original_Content(){
			using var application=DocumentStyleManagerModule().Application;
			MockDocumentsListEditor(application);

			var tuple = application.SetApplyTemplateStyleDetailView(Document);
			var detailView = ((DetailView) tuple.window.View);
			var documentsListView = detailView.DocumentsListView();
			documentsListView.Editor.GetMock()
				.Setup(editor => editor.GetSelectedObjects())
				.Returns(() => ((BusinessObjects.ApplyTemplateStyle) detailView.CurrentObject).Documents.Take(1).ToArray());

			documentsListView.OnSelectionChanged();

			var applyTemplateStyle = tuple.applyTemplateStyle;

			applyTemplateStyle.Documents.Any(document => applyTemplateStyle.Original!=null).ShouldBeTrue();
			applyTemplateStyle.Documents.Any(document => applyTemplateStyle.Changed!=null).ShouldBeTrue();
		}

		[TestCase(DocumentStyleLinkOperation.Ensure)]
		[TestCase(DocumentStyleLinkOperation.Replace)]
        [XpandTest()][Apartment(ApartmentState.STA)]
		public void ApplyTemplate_When_Selected_TemplateDocument_Changed_and_template_set(DocumentStyleLinkOperation operation){
			using var application=DocumentStyleManagerModule().Application;
			ApplyTemplate(frame => {
				var documentsListView = frame.View.AsDetailView()
					.GetListPropertyEditor<BusinessObjects.ApplyTemplateStyle>(_ => _.Documents).Frame.View.AsListView();
				documentsListView.OnSelectionChanged();
			},operation,application);
		}


		[Test][XpandTest()][Apartment(ApartmentState.STA)]
		public void Remove_Unused_When_ApplyTemplate(){
			using var application=DocumentStyleManagerModule().Application;
			MockDocumentsListEditor(application);

			var documentStyle = Document.NewDocumentStyle(1, DocumentStyleType.Paragraph,true).Select(s => s.ToDocumentStyle(Document)).First();
			var tuple = application.SetApplyTemplateStyleDetailView(Document);
			var applyTemplateStyle = tuple.applyTemplateStyle;
			var detailView = ((DetailView) tuple.window.View);
			var documentsListView = detailView.DocumentsListView();
			documentsListView.Editor.GetMock()
				.Setup(editor => editor.GetSelectedObjects())
				.Returns(() => ((BusinessObjects.ApplyTemplateStyle) detailView.CurrentObject).Documents.Take(1).ToArray());
			var objectSpace = application.CreateObjectSpace();
			applyTemplateStyle.Template = objectSpace.CreateObject<DocumentStyleLinkTemplate>();

			documentsListView.OnSelectionChanged();

			var changedDocument = tuple.window.View.AsDetailView().ApplyTemplateStyleChangedRichEditControl().Document;
			changedDocument.ParagraphStyles.FirstOrDefault(style => style.Name==documentStyle.StyleName&&style.IsDeleted).ShouldNotBeNull();
		}


	}
}