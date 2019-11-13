using System;
using System.Linq;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EasyTest.WebAdapter;
using DevExpress.ExpressApp.EasyTest.WebAdapter.Commands;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AppDomain;
using Xpand.TestsLib;
using Xpand.TestsLib.EasyTest;
using Xpand.XAF.Modules.Reactive;

namespace ALL.Web.Tests{
    [NonParallelizable]
    public class AllWebTests : BaseTest{
        [Test()]
        [TestCaseSource(nameof(AgnosticModules))]
        [TestCaseSource(nameof(WebModules))]
        public void UnloadWebModules(Type moduleType){
            ReactiveModuleBase.Unload(moduleType);
            using (var application = new TestWebApplication(moduleType, false)){
                application.AddModule((ModuleBase) moduleType.CreateInstance(), nameof(UnloadWebModules));

                application.Modules.FirstOrDefault(m => m.GetType()==moduleType).ShouldBeNull();
            }
        }
        [Test]
        public void Web_EasyTest(){
            var webAdapter = new WebAdapter();
            var testApplication = webAdapter.RunWebApplication($@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWebApplication\","65377");

            var commandAdapter = webAdapter.CreateCommandAdapter();
            
            var autoTestCommand = new AutoTestCommand();
            autoTestCommand.Execute(commandAdapter);

            webAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);

        }

    }
}