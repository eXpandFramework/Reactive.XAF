using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win.Editors;
using Shouldly;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.ApplyTemplateStyle{
	public abstract class BaseTests:Tests.BaseTests{
		protected void MockDocumentsListEditor(XafApplication xafApplication){
			// xafApplication.MockEditorsFactory();
			xafApplication.MockListEditor((view, application, collectionsource) =>
				view.Id.Contains(nameof(Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects.ApplyTemplateStyle.Documents))
					? xafApplication.ListEditorMock(view).Object
					: new GridListEditor(view));
		}

		protected void ApplyTemplate(Action<Frame> when,DocumentStyleLinkOperation operation,XafApplication application){
			MockDocumentsListEditor(application);

			var style = Document.NewDocumentStyle(1, DocumentStyleType.Paragraph).Select(s => s.ToDocumentStyle(Document)).First();
			Document.Paragraphs.Append();
			Document.Paragraphs.Last().Style = Document.ParagraphStyles.First();

			var documentStyles = new[]{style,Document.NewParagraphStyle(1,operation==DocumentStyleLinkOperation.Ensure).ToDocumentStyle(Document)}.ToArray();
			var tuple = application.SetApplyTemplateStyleDetailView(Document);
			var applyTemplateStyle = tuple.applyTemplateStyle;
			var detailView = ((DetailView) tuple.window.View);
			
			detailView.DocumentsListView().Editor.GetMock()
				.Setup(editor => editor.GetSelectedObjects())
				.Returns(() => ((Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects.ApplyTemplateStyle) detailView.CurrentObject).Documents.Take(1).ToArray());

			void ConfigureApplyTemplateStyle(){
				var objectSpace = application.CreateObjectSpace();
				applyTemplateStyle.Template = objectSpace.CreateObject<DocumentStyleLinkTemplate>();
				var documentStyleLink = objectSpace.CreateObject<DocumentStyleLink>();
				
				documentStyleLink.Original = documentStyles.First().NewTemplateStyle(objectSpace);
				documentStyleLink.Replacement = new DocumentStyle(){DocumentStyleType = DocumentStyleType.Paragraph, StyleName = "Rep1"}.NewTemplateStyle(objectSpace);
				applyTemplateStyle.Template.DocumentStyleLinks.Add(documentStyleLink);
				documentStyleLink = objectSpace.CreateObject<DocumentStyleLink>();
				if (operation==DocumentStyleLinkOperation.Replace){
					documentStyleLink.Original = documentStyles.Last().NewTemplateStyle(objectSpace);
				}
				documentStyleLink.Replacement = new DocumentStyle(){DocumentStyleType = DocumentStyleType.Paragraph, StyleName = "Rep2"}.NewTemplateStyle(objectSpace);
				documentStyleLink.Operation=operation;
				applyTemplateStyle.Template.DocumentStyleLinks.Add(documentStyleLink);
			
				
			}

			ConfigureApplyTemplateStyle();
			var changedDocument = tuple.window.View.AsDetailView().ApplyTemplateStyleChangedRichEditControl().Document;

			when(tuple.window);

			var repStyle = changedDocument.ParagraphStyles.FirstOrDefault(_ => _.Name == "Rep1");
			repStyle.ShouldNotBeNull();

			repStyle = changedDocument.ParagraphStyles.FirstOrDefault(_ => _.Name == "Rep2");
			repStyle.ShouldNotBeNull();	
			applyTemplateStyle.ChangedStyles.Count.ShouldBe(2);
			var changedStyle = applyTemplateStyle.ChangedStyles.FirstOrDefault(link => link.Replacement.StyleName=="Rep1");
			changedStyle.ShouldNotBeNull();
			 changedStyle.Count.ShouldBe(1);
			changedStyle = applyTemplateStyle.ChangedStyles.FirstOrDefault(link => link.Replacement.StyleName=="Rep2");
			changedStyle.ShouldNotBeNull();
			changedStyle.Count.ShouldBe(1);
		}

	}
}