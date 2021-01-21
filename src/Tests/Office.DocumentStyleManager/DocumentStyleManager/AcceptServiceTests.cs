using System.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.Data.Filtering;
using DevExpress.XtraRichEdit;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.DocumentStyleManager{
	public class AcceptServiceTests:CommonTests{
        [Test][Apartment(ApartmentState.STA)][XpandTest()]
        public void AcceptChangesAction_updates_caller_member_value_and_close_view(){
            using var application=DocumentStyleManagerModule().Application;
            var dataObject = application.CreateObjectSpace().CreateObject<DataObject>();
            dataObject.Content=Document.ToByteArray(DocumentFormat.OpenXml);
            dataObject.Name = "aaaa";
            dataObject.ObjectSpace.CommitChanges();
            application.Model.DocumentStyleManager().DefaultPropertiesProviderCriteria =
	           CriteriaOperator.Parse("Oid=?", dataObject.Oid).ToString();
            var window = application.ShowDocumentStyleManagerDetailView(dataObject);
            
            var documentStyleManager = ((BusinessObjects.DocumentStyleManager) window.View.CurrentObject);
            Document.NewDocumentStyle(1, DocumentStyleType.Paragraph);
            documentStyleManager.Content = Document.ToByteArray(DocumentFormat.OpenXml);
            var viewCLosingObserver = window.View.WhenClosing().Test();
            var acceptAction = window.Action<DocumentStyleManagerModule>().AcceptStyleManagerChanges();

            acceptAction.DoExecute();

            dataObject.Content.ShouldBe(documentStyleManager.Content);
            RichEditDocumentServerExtensions.LoadDocument(RichEditDocumentServer, dataObject.Content).ParagraphStyles
                .FirstOrDefault(style => style.Name.StartsWith(TestExtensions.PsStyleName)).ShouldNotBeNull();
            viewCLosingObserver.Items.Count.ShouldBe(1);
        }

        [Test][Apartment(ApartmentState.STA)][XpandTest()]
        public void Accept_Changes_Saves_DocumentStyleLinkTemplate(){
            using var application=DocumentStyleManagerModule().Application;
	        var dataObject = application.CreateObjectSpace().CreateObject<DataObject>();
	        dataObject.Content=Document.ToByteArray(DocumentFormat.OpenXml);
	        var window = application.ShowDocumentStyleManagerDetailView(dataObject);
	        var objectSpace = application.CreateObjectSpace();
	        var template = objectSpace.CreateObject<DocumentStyleLinkTemplate>();
            objectSpace.CommitChanges();
	        ((BusinessObjects.DocumentStyleManager) window.View.CurrentObject).DocumentStyleLinkTemplate = template;

            template.DocumentStyleLinks.Add(objectSpace.CreateObject<DocumentStyleLink>());

            window.Action<DocumentStyleManagerModule>().AcceptStyleManagerChanges().DoExecute();

            application.CreateObjectSpace().GetObject(template).DocumentStyleLinks.Count.ShouldBe(1);
        }

        [Test][Apartment(ApartmentState.STA)][XpandTest()]
        public void CancelAction_close_view_but_not_update_caller_object(){
            using var application=DocumentStyleManagerModule().Application;
            var dataObject = application.CreateObjectSpace().CreateObject<DataObject>();
            dataObject.Content=Document.ToByteArray(DocumentFormat.OpenXml);
            var window = application.ShowDocumentStyleManagerDetailView(dataObject);
            
            var documentStyleManager = ((BusinessObjects.DocumentStyleManager) window.View.CurrentObject);
            Document.NewDocumentStyle(1, DocumentStyleType.Paragraph);
            documentStyleManager.Content = Document.ToByteArray(DocumentFormat.OpenXml);
            var viewCLosingObserver = window.View.WhenClosing().Test();
            var cancelAction = window.Action<DocumentStyleManagerModule>().CancelStyleManagerChanges();

            cancelAction.DoTheExecute();
            
            dataObject.Content.ShouldNotBe(documentStyleManager.Content);
            RichEditDocumentServerExtensions.LoadDocument(RichEditDocumentServer, dataObject.Content).ParagraphStyles
                .FirstOrDefault(style => style.Name.StartsWith(TestExtensions.PsStyleName)).ShouldBeNull();
            viewCLosingObserver.Items.Count.ShouldBe(1);
        }

    }
}