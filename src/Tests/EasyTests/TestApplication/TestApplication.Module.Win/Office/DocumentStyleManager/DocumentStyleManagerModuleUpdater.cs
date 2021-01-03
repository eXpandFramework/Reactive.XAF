using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Xpand.Extensions.StreamExtensions;

namespace TestApplication.Module.Win.Office.DocumentStyleManager{
    public class DocumentStyleManagerModuleUpdater:ModuleUpdater{
        public DocumentStyleManagerModuleUpdater(IObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace, currentDBVersion){
        }

        public override void UpdateDatabaseAfterUpdateSchema(){
            base.UpdateDatabaseAfterUpdateSchema();
            if (!ObjectSpace.GetObjectsQuery<DocumentObject>().Any()){
                var assembly = GetType().Assembly;
                var documentObject = ObjectSpace.CreateObject<DocumentObject>();
                documentObject.Name = "Test document 1";
                documentObject.Content = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().First(s => s.Contains("Lorem"))).Bytes();
                documentObject = ObjectSpace.CreateObject<DocumentObject>();
                documentObject.Name = "Test document 2";
                documentObject.Content = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().First(s => s.Contains("Lorem2"))).Bytes();
                documentObject = ObjectSpace.CreateObject<DocumentObject>();
                documentObject.Name = "Test document 3";
                documentObject.Content = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().First(s => s.Contains("Lorem3"))).Bytes();
                documentObject = ObjectSpace.CreateObject<DocumentObject>();
                documentObject.Name = "Template";
                documentObject.Content = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().First(s => s.Contains("LoresumV2"))).Bytes();
                ObjectSpace.CommitChanges();
            }
        }
    }
}