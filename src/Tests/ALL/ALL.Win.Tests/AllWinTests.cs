using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ALL.Tests;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EasyTest.WinAdapter;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.XAF.Modules.Reactive;

namespace ALL.Win.Tests{
	[NonParallelizable]
    public class AllWinTests : BaseTest{
        [Test()]
        [TestCaseSource(nameof(AgnosticModules))]
        [TestCaseSource(nameof(WinModules))]
        [XpandTest]
        public void UnloadWinModules(Type moduleType){
            ReactiveModuleBase.Unload(moduleType);
            using (var application = new TestWinApplication(moduleType, false)){
                application.AddModule((ModuleBase) moduleType.CreateInstance(), nameof(UnloadWinModules));

                application.Modules.FirstOrDefault(m => m.GetType()==moduleType).ShouldBeNull();
            }
        } 
        [Test]
        [XpandTest(LongTimeout,3)]
        [Apartment(ApartmentState.STA)]
        public async Task Win_EasyTest(){
            using (var winAdapter = new WinAdapter()){
                var testApplication = winAdapter.RunWinApplication($@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWinApplication\TestApplication.Win.exe");
                try{
                    var commandAdapter = winAdapter.CreateCommandAdapter();
                    commandAdapter.Execute(new LoginCommand());
                    var autoTestCommand = new AutoTestCommand();
                    autoTestCommand.Execute(commandAdapter);
                    await commandAdapter.TestMicrosoftService();
                }
                finally{
                    winAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);    
                }
            }
        }

    }
}