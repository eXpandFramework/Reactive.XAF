using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraRichEdit;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.DocumentStyleManager{
	public class TemplateStyleSelectionServiceTests:BaseTests{
		[Test][Apartment(ApartmentState.STA)][XpandTest()]
		public void Action_should_be_disable_by_default(){
			using var application=DocumentStyleManagerModule().Application;
			var dataObject = application.CreateObjectSpace().CreateObject<DataObject>();
			dataObject.Content=Document.ToByteArray(DocumentFormat.OpenXml);
			var window = application.ShowDocumentStyleManagerDetailView(dataObject);

			window.Action<DocumentStyleManagerModule>().TemplateStyleSelection().Enabled["ByAppearance"].ShouldBeFalse();

		}
		[Test][Apartment(ApartmentState.STA)][XpandTest()]
		public void Action_should_be_enabled_when_templatelink_has_value(){
			using var application=DocumentStyleManagerModule().Application;
			var dataObject = application.CreateObjectSpace().CreateObject<DataObject>();
			dataObject.Content=Document.ToByteArray(DocumentFormat.OpenXml);
			var window = application.ShowDocumentStyleManagerDetailView(dataObject);
			var documentStyleManager = ((BusinessObjects.DocumentStyleManager) window.View.CurrentObject);

			documentStyleManager.DocumentStyleLinkTemplate =
				application.CreateObjectSpace().CreateObject<DocumentStyleLinkTemplate>();

			var boolList = window.Action<DocumentStyleManagerModule>().TemplateStyleSelection().Enabled;
			boolList.ResultValue.ShouldBe(true);
			documentStyleManager.DocumentStyleLinkTemplate = null;
			boolList.ResultValue.ShouldBe(false);

		}

		[Test][XpandTest()]
		public void Action_SelectionContext_should_be_the_ReplacemenntStyles_ListView(){
			using var application=DocumentStyleManagerModule().Application;
			var tuple = application.SetDocumentStyleManagerDetailView(Document);

			tuple.window.Action<DocumentStyleManagerModule>().TemplateStyleSelection().SelectionContext.ShouldBe(tuple.window.View
				.AsDetailView().GetListPropertyEditor<BusinessObjects.DocumentStyleManager>(_ => _.ReplacementStyles).Frame.View
				.AsListView());
		}
		
		[Test]
        [XpandTest()]
		public void Action_Enable_is_bound_to_ReplaceStyles_Enable(){
			using var application=DocumentStyleManagerModule().Application;
			application.MockListEditor((view, xafApplication, arg3) => new GridListEditor(view));
			var tuple = application.SetDocumentStyleManagerDetailView(Document);

			var templateStyleSelection = tuple.window.Action<DocumentStyleManagerModule>().TemplateStyleSelection();
			templateStyleSelection.Enabled[nameof(ReplaceStylesService.ReplaceStyles)].ShouldBeFalse();
			
			tuple.window.Action<DocumentStyleManagerModule>().ReplaceStyles().Enabled.Clear();
			templateStyleSelection.Enabled[nameof(ReplaceStylesService.ReplaceStyles)].ShouldBeTrue();
			
		}

		[TestCase(DocumentStyleType.Character)]
		[TestCase(DocumentStyleType.Paragraph)][XpandTest()]
		public void New_TemplateStyle_Stores_Parent_Styles_Seperately(DocumentStyleType type){
			using var application=DocumentStyleManagerModule().Application;
			var parentStyle = new DocumentStyle(){DocumentStyleType = type, StyleName = "parent", FontName = "test"};
			var childStyle = new DocumentStyle(){DocumentStyleType = type, StyleName = "child", Parent = parentStyle};

			var templateStyle = childStyle.NewTemplateStyle(application.CreateObjectSpace());

			templateStyle.Parent.ShouldNotBeNull();
			templateStyle.DocumentStyleType.ShouldBe(type);
			templateStyle.Parent.StyleName.ShouldBe(parentStyle.StyleName);
			templateStyle.Parent.FontName.ShouldBe(parentStyle.FontName);
			templateStyle.StyleName.ShouldBe(childStyle.StyleName);
			templateStyle.FontName.ShouldBeNull();
		}
		
		[TestCase(DocumentStyleType.Paragraph)][XpandTest()]
		public void Existing_TemplateStyle_Reflects_Parent_Style_Properties(DocumentStyleType type){
			using var application=DocumentStyleManagerModule().Application;
			using var objectSpace = application.CreateObjectSpace();
			var parentTemplate = objectSpace.CreateObject<TemplateStyle>();
			parentTemplate.StyleName = "parent";
			parentTemplate.FontName = "test";
			var childTemplate = objectSpace.CreateObject<TemplateStyle>();
			childTemplate.StyleName = "child";
			childTemplate.Parent=parentTemplate;
			var documentStyleLink = objectSpace.CreateObject<DocumentStyleLink>();
			documentStyleLink.Original=childTemplate;

			documentStyleLink.SetDefaultPropertiesProvider(Document);
			var originalStyle = documentStyleLink.OriginalStyle;
				
			originalStyle.StyleName.ShouldBe(childTemplate.StyleName);
			originalStyle.FontName.ShouldBe(parentTemplate.FontName);
			originalStyle.Parent.ShouldNotBeNull();
			originalStyle.Parent.FontName.ShouldBe(parentTemplate.FontName);
		}

		[Test][Apartment(ApartmentState.STA)][XpandTest()]
		public void Action_adds_selected_styles(){
			using var xafApplication=DocumentStyleManagerModule().Application;
			// application.MockEditorsFactory();
			xafApplication.MockListEditor((view, application, collectionsource) => {
				if (view.Id.Contains(nameof(BusinessObjects.DocumentStyleManager.AllStyles)))
					return application.ListEditorMock(view).Object;
				if (view.Id.Contains(nameof(BusinessObjects.DocumentStyleManager.ReplacementStyles)))
					return application.ListEditorMock(view).Object;
				return new GridListEditor(view);
			});
            
			var server = xafApplication.WhenDetailViewCreated().SelectMany(_ => _.e.View.WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(manager => manager.Content)).Test();
			var tuple = xafApplication.SetDocumentStyleManagerDetailView(Document);
			var documentStyleManager = tuple.documentStyleManager;
			var document = server.Items.First().Document;
			document.NewDocumentStyle(2, DocumentStyleType.Paragraph);
			documentStyleManager.Content = document.ToByteArray(DocumentFormat.OpenXml);
			var allStylesListEditorMock = tuple.window.View.AsDetailView().AllStylesListView().Editor;
			allStylesListEditorMock.GetMock()
				.Setup(editor => editor.GetSelectedObjects())
				.Returns(() => documentStyleManager.AllStyles.Where(style =>
				style.DocumentStyleType == DocumentStyleType.Paragraph).ToArray());
			var replacementStyle = documentStyleManager.ReplacementStyles.WhenNotDefaultStyle().Skip(1).Take(1).First();
			var replacementStylesListEditorMock = tuple.window.View.AsDetailView().ReplacementStylesListView().Editor;
			replacementStylesListEditorMock.GetMock()
				.Setup(editor => editor.GetSelectedObjects())
				.Returns(() => new object[]{replacementStyle});
			var templateStyleSelection = tuple.window.Action<DocumentStyleManagerModule>().TemplateStyleSelection();
			var objectSpace = xafApplication.CreateObjectSpace();
			documentStyleManager.DocumentStyleLinkTemplate = objectSpace.CreateObject<DocumentStyleLinkTemplate>();
			objectSpace.CommitChanges();

			templateStyleSelection.DoTheExecute(true);


			var documentStyleLinks = documentStyleManager.DocumentStyleLinkTemplate.DocumentStyleLinks;
			documentStyleLinks.Count.ShouldBeGreaterThan(0);
			var allStyles = allStylesListEditorMock.GetSelectedObjects().Cast<DocumentStyle>().ToArray();
			documentStyleLinks.Count.ShouldBe(allStyles.Length);
			var documentStyleLink = documentStyleLinks.First();
			documentStyleLink.Original.ShouldNotBeNull();
			documentStyleLink.OriginalStyle.ShouldNotBeNull();
			documentStyleLink.Original.StyleName.ShouldBe(allStyles.First().StyleName);
			documentStyleLink.OriginalStyle.StyleName.ShouldBe(allStyles.First().StyleName);
			documentStyleLink.Replacement.ShouldNotBeNull();
			documentStyleLink.ReplacementStyle.ShouldNotBeNull();
			documentStyleLink.Replacement.StyleName.ShouldBe(replacementStylesListEditorMock
				.GetSelectedObjects().Cast<DocumentStyle>().First().StyleName);
			documentStyleLink.ReplacementStyle.StyleName.ShouldBe(replacementStylesListEditorMock
				.GetSelectedObjects().Cast<DocumentStyle>().First().StyleName);
		}

	}
}