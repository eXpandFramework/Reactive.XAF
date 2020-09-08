using System;
using System.Linq;
using System.Reactive.Linq;
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
using Xpand.Extensions.Reactive.Transform;
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
            await EasyTest(() => new WinAdapter(), RunWinApplication, adapter => {
                    var autoTestCommand = new AutoTestCommand("Event|Task|Reports");
                    adapter.Execute(autoTestCommand);
                    return Task.CompletedTask;
            });
        }     

        [Test]
        [XpandTest(LongTimeout,3)]
        [Apartment(ApartmentState.STA)]
        public async Task Win_EasyTest_Google(){
            await EasyTest(() => new WinAdapter(), RunWinApplication, async adapter => {
                await adapter.TestGoogleService(() => Observable.Start(adapter.TestGoogleTasksService).ToUnit());
                });
        }     

        [Test]
        [XpandTest(LongTimeout,3)]
        [Apartment(ApartmentState.STA)]
        public async Task Win_EasyTest_Microsoft(){
            await EasyTest(() => new WinAdapter(), RunWinApplication, async adapter => {
                await adapter.TestMicrosoftService(() => Observable.Start(() => {
                    adapter.TestMicrosoftCalendarService();
                    adapter.TestMicrosoftTodoService();
                }).ToUnit());
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