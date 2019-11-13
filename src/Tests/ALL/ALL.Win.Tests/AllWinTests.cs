using System;
using System.Linq;
using System.Threading;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EasyTest.WinAdapter;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AppDomain;
using Xpand.TestsLib;
using Xpand.TestsLib.EasyTest;
using Xpand.XAF.Modules.Reactive;

namespace ALL.Win.Tests{
    [NonParallelizable]
    public class AllWinTests : BaseTest{
        [Test()]
        [TestCaseSource(nameof(AgnosticModules))]
        [TestCaseSource(nameof(WinModules))]
        public void UnloadWinModules(Type moduleType){
            ReactiveModuleBase.Unload(moduleType);
            using (var application = new TestWinApplication(moduleType, false)){
                application.AddModule((ModuleBase) moduleType.CreateInstance(), nameof(UnloadWinModules));

                application.Modules.FirstOrDefault(m => m.GetType()==moduleType).ShouldBeNull();
            }
        }
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Win_EasyTest(){
            var winAdapter = new WinAdapter();
            var testApplication = winAdapter.RunWinApplication($@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWinApplication\TestApplication.Win.exe");

            var commandAdapter = winAdapter.CreateCommandAdapter();
            
            var autoTestCommand = new AutoTestCommand();
            autoTestCommand.Execute(commandAdapter);

            winAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);

        }

    }
}