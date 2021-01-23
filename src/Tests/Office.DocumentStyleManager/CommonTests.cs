using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Office.Win;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using NUnit.Framework;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.TestsLib.Net461;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive;
[assembly:XpandTest(IgnoredXAFMinorVersions="19.1")]
namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests{
    public abstract class CommonTests:BaseTest{
        
        protected RichEditDocumentServer RichEditDocumentServer;
        protected Document Document;

        [SetUp]
        public override void Setup(){
            base.Setup();
	        RichEditDocumentServer = new RichEditDocumentServer();
	        RichEditDocumentServer.CreateNewDocument();
	        Document = RichEditDocumentServer.Document;
        }
        protected DocumentStyleManagerModule DocumentStyleManagerModule(params ModuleBase[] modules){
            var application = Platform.Win.NewApplication<DocumentStyleManagerModule>();
            application.Modules.Add(new OfficeWindowsFormsModule());
            application.Modules.AddRange(modules);
            var documentStyleManagerModule = application.AddModule<DocumentStyleManagerModule>(typeof(DataObject),typeof(DataObjectParent));
            var xafApplication = documentStyleManagerModule.Application;
            
            
            var modelOfflineModule = xafApplication.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.DocumentStyleManager();
            modelOfflineModule.DefaultPropertiesProvider = xafApplication.Model.BOModel.GetClass(typeof(DataObject));
            using var objectSpace = xafApplication.CreateObjectSpace();
            var dataObject = objectSpace.CreateObject<DataObject>();
            dataObject.Content = Document.ToByteArray(DocumentFormat.OpenXml);
            objectSpace.CommitChanges();
            modelOfflineModule.DefaultPropertiesProviderCriteria =
                CriteriaOperator.Parse("Oid=?", dataObject.Oid).ToString();
            
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return documentStyleManagerModule;
        }

        public override void Dispose(){
            base.Dispose();
            RichEditDocumentServer?.Dispose();
        }

    }
}