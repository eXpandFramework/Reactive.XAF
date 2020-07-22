using System;
using System.Linq;
using System.Reactive.Linq;
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
using Xpand.TestsLib.EasyTest.Commands;
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
        [Test][Apartment(ApartmentState.STA)]
        public async Task Web_EasyTest(){
            using (var webAdapter = new WebAdapter()){
                var testApplication = webAdapter.RunWebApplication($@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWebApplication\",65477);
                try{
                    
	                var commandAdapter = webAdapter.CreateCommandAdapter();
                    commandAdapter.Execute(new LoginCommand());
                    commandAdapter.TestLookupCascade();
                    await commandAdapter.TestMicrosoftService(() => Observable.Start(() => {
                        commandAdapter.TestMicrosoftCalendarService();
                        commandAdapter.TestMicrosoftTodoService();
                        
                    }));
                    
                }
                finally{
                    webAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);
                }
            }
        }

    }

}