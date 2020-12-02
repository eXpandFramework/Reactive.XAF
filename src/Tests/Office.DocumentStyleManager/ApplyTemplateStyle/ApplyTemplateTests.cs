using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using DevExpress.XtraGrid;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.ApplyTemplateStyle{
	public class ApplyTemplateTests:BaseTests{

		[Test][XpandTest()][Apartment(ApartmentState.STA)]
		public void ApplyTemplateAction_Is_enabled_when_template_has_value() {
			var type = typeof(TestWinApplication);
			new GridControl();
			using var application=DocumentStyleManagerModule().Application;
			var tuple = application.SetApplyTemplateStyleDetailView();
			var applyTemplate = tuple.window.Action<DocumentStyleManagerModule>().ApplyTemplate();

			applyTemplate.Enabled["ByAppearance"].ShouldBeFalse();

			tuple.applyTemplateStyle.Template = application.CreateObjectSpace().CreateObject<DocumentStyleLinkTemplate>();
			applyTemplate.Enabled.ResultValue.ShouldBeTrue();
		}
		
		[TestCase(DocumentStyleLinkOperation.Replace)]
		[TestCase(DocumentStyleLinkOperation.Ensure)]
        [XpandTest()][Apartment(ApartmentState.STA)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public void When_ApplyTemplateAction_Executed_Applies_active_template_to_all_and_saves_changes_to_source_objects(DocumentStyleLinkOperation operation){
			using var application=DocumentStyleManagerModule().Application;
			ApplyTemplate(frame => {
                var objectSpace = application.CreateObjectSpace();
				var modelDocumentStyleManager = application.Model.DocumentStyleManager();
                var oid = objectSpace.GetObjects(modelDocumentStyleManager.DefaultPropertiesProvider.TypeInfo.Type,
                        objectSpace.ParseCriteria(modelDocumentStyleManager.DefaultPropertiesProviderCriteria)).Cast<DataObject>().First().Oid;
				var content = objectSpace.GetObjectsQuery<DataObject>().ToArray().First(o => o.Oid != oid).Content;
				frame.Action<DocumentStyleManagerModule>().ApplyTemplate().DoExecute();
				content.ShouldNotBe(application.CreateObjectSpace().GetObjectByKey<DataObject>(oid).Content);
			},operation,application);
		}


	}
}