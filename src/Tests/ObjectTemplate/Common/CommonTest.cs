using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;

namespace Xpand.XAF.Modules.ObjectTemplate.Tests.Common {
    public abstract class CommonTest : BlazorCommonTest {
        

        [OneTimeTearDown]
        public override void Cleanup(){
            
        }
        [OneTimeSetUp]
        public override void Init(){
            
        }
        
        public ObjectTemplateModule ObjectTemplateModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication();
            return ObjectTemplateModule(newBlazorApplication);
        }
        
        protected BlazorApplication NewBlazorApplication() => NewBlazorApplication(typeof(NotificationStartup));

        protected ObjectTemplateModule ObjectTemplateModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<ObjectTemplateModule>(GetType().CollectExportedTypesFromAssembly().ToArray());
            // newBlazorApplication.ConfigureModel();
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }



    }
}