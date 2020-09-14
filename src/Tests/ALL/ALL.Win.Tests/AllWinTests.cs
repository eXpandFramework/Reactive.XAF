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
using Xpand.XAF.Modules.Reactive;
using AutoTestCommand = Xpand.TestsLib.EasyTest.Commands.ActionCommands.AutoTestCommand;
using BaseTest = ALL.Tests.BaseTest;

namespace ALL.Win.Tests{
	[NonParallelizable]
    public class 
        AllWinTests : BaseTest{
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
        public async Task Win_EasyTest_InMemory(){
            await EasyTest(() => new WinAdapter(), RunWinApplication, async adapter => {
                    var autoTestCommand = new AutoTestCommand("Event|Task|Reports");
                    adapter.Execute(autoTestCommand);
                    await adapter.TestCloudServices();
                    // adapter.TestViewWizardService();
            });
        }     

        private static TestApplication RunWinApplication(WinAdapter adapter, string connectionString) 
            => adapter.RunWinApplication(
                $@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWinApplication\TestApplication.Win.exe",connectionString);


        [Test]
        [XpandTest(LongTimeout,3)]
        [Apartment(ApartmentState.STA)]
        public async Task Win_EasyTest_InLocalDb(){
            var connectionString = "Integrated Security=SSPI;Pooling=false;Data Source=(localdb)\\mssqllocaldb;Initial Catalog=TestApplicationWin";
            await EasyTest(() => new WinAdapter(), RunWinApplication, adapter => {
                adapter.TestSequenceGeneratorService();
                return Task.CompletedTask;
            
            },connectionString);
            
        }

    }
}