using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ALL.Tests;
using ALL.Win.Tests;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EasyTest.WebAdapter;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using BaseTest = ALL.Tests.BaseTest;

namespace ALL.Web.Tests{
    [NonParallelizable]
    public class AllWebTests : BaseTest{
        [Test()]
        [TestCaseSource(nameof(AgnosticModules))]
        [TestCaseSource(nameof(WebModules))]
        [XpandTest]
        public void UnloadWebModules(Type moduleType){
            ReactiveModuleBase.Unload(moduleType);
            using var application = new TestWebApplication(moduleType, false);
            application.AddModule((ModuleBase) moduleType.CreateInstance(), nameof(UnloadWebModules));

            application.Modules.FirstOrDefault(m => m.GetType()==moduleType).ShouldBeNull();
        }

        private TestApplication RunWebApplication(WebAdapter adapter, string connectionString){
            var physicalPath = $@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWebApplication\";
            LogPaths.Clear();
            LogPaths.Add(Path.Combine(Path.GetDirectoryName(physicalPath)!,"eXpressAppFramework.log"));
            LogPaths.Add(Path.Combine(@$"{Path.GetDirectoryName(physicalPath)}\bin",Path.GetFileName(ReactiveLoggerService.RXLoggerLogPath)));
            return adapter.RunWebApplication(physicalPath,
                65477, connectionString);
        }

        [XpandTest(LongTimeout,3)]
        [Test][Apartment(ApartmentState.STA)]
        public async Task Web_EasyTest_InMemory(){
            await EasyTest(() => new WebAdapter(), RunWebApplication,  adapter => {
                var autoTestCommand = new AutoTestCommand("Event|Task|Reports");
                adapter.Execute(autoTestCommand);
                return Task.CompletedTask;
            });
        }

        [XpandTest(LongTimeout,3)]
        [Test][Apartment(ApartmentState.STA)]
        public async Task Web_MicrosoftCloud_EasyTest(){
            DeleteBrowserFiles.Execute();
            await EasyTest(() => new WebAdapter(), RunWebApplication, async adapter => {
                await adapter.TestMicrosoftService(async () => {
                    await adapter.TestMicrosoftCalendarService();
                    await adapter.TestMicrosoftTodoService();
                });
            });
        }

        [XpandTest(LongTimeout,3)]
        [Test][Apartment(ApartmentState.STA)]
        public async Task Web_GoogleCloud_EasyTest(){
            DeleteBrowserFiles.Execute();
            await EasyTest(() => new WebAdapter(), RunWebApplication, async adapter => {
                await adapter.TestGoogleService(async () => {
                    await adapter.TestGoogleCalendarService();
                    await adapter.TestGoogleTasksService();
                });
            });
        }

        [Test]
        [XpandTest(LongTimeout,3)]
        [Apartment(ApartmentState.STA)]
        public async Task Web_EasyTest_InLocalDb(){
            var connectionString = "Integrated Security=SSPI;Pooling=false;Data Source=(localdb)\\mssqllocaldb;Initial Catalog=TestApplicationWeb";
            await EasyTest(() => new WebAdapter(), RunWebApplication,  adapter => {
                adapter.TestSequenceGeneratorService();
                return Task.CompletedTask;
            
            },connectionString);
            
        }


    }

}