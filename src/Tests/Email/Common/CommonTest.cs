using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;

namespace Xpand.XAF.Modules.Email.Tests.Common {
    public abstract class CommonTest : BlazorCommonTest {
        

        [OneTimeTearDown]
        public override void Cleanup(){
            
        }
        [OneTimeSetUp]
        public override void Init(){
            
        }
        
        public EmailModule EmailModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication();
            return EmailModule(newBlazorApplication);
        }
        
        protected BlazorApplication NewBlazorApplication() => NewBlazorApplication(typeof(Startup));

        protected EmailModule EmailModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<EmailModule>(GetType().CollectExportedTypesFromAssembly().ToArray());
            // newBlazorApplication.ConfigureModel();
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }



    }
}