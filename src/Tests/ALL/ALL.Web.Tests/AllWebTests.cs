using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using Xpand.XAF.Modules.Reactive;
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
            using (var application = new TestWebApplication(moduleType, false)){
                application.AddModule((ModuleBase) moduleType.CreateInstance(), nameof(UnloadWebModules));

                application.Modules.FirstOrDefault(m => m.GetType()==moduleType).ShouldBeNull();
            }
        }
        private static TestApplication RunWinApplication(WebAdapter adapter, string connectionString) 
            => adapter.RunWebApplication($@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWebApplication\",65477);

        [XpandTest(LongTimeout,3)]
        [Test][Apartment(ApartmentState.STA)]
        public async Task Web_EasyTest(){
            await EasyTest(() => new WebAdapter(), RunWinApplication, async adapter => {
                var autoTestCommand = new AutoTestCommand("Event|Task");
                adapter.Execute(autoTestCommand);
                await adapter.TestCloudServices();
            });
        }

    }

}