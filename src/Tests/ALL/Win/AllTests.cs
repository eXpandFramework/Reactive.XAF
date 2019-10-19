using System;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Reactive;

namespace ALL.Win.Tests{
    [NonParallelizable]
    public class AllTests : BaseTest{
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

        
    }
}