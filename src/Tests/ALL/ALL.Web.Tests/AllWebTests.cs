using System;
using System.Linq;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EasyTest.WebAdapter;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AppDomain;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.TestsLib.EasyTest;
using Xpand.XAF.Modules.Reactive;

namespace ALL.Web.Tests{
    [NonParallelizable]
    public class AllWebTests : BaseTest{
        [Test()]
        [TestCaseSource(nameof(AgnosticModules))]
        [TestCaseSource(nameof(WebModules))]
        [XpandTest]
        public void UnloadWebModules(Type moduleType){
            ReactiveModuleBase.Unload(moduleType);
            using (var application = new TestWebApplication(moduleType, false)){
                application.AddModule((ModuleBase) moduleType.CreateInstance(), nameof(UnloadWebModules));

                application.Modules.FirstOrDefault(m => m.GetType()==moduleType).ShouldBeNull();
            }
        }
        [XpandTest(LongTimeout,3)]
        [Test]
        public void Web_EasyTest(){
            using (var webAdapter = new WebAdapter()){
                var testApplication = webAdapter.RunWebApplication($@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWebApplication\",65377);
                var commandAdapter = webAdapter.CreateCommandAdapter();

                try{
                    commandAdapter.TestLookupCascade();
                }
                finally{
                    webAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);
                }
            }
        }

    }

}