using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EasyTest.WinAdapter;
using DevExpress.ExpressApp.Xpo;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AppDomain;
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
            using (var application = new Xpand.TestsLib.TestWinApplication(moduleType, false)){
                application.AddModule((ModuleBase) moduleType.CreateInstance(), nameof(UnloadWinModules));

                application.Modules.FirstOrDefault(m => m.GetType()==moduleType).ShouldBeNull();
            }
        }
        [Test]
        [Apartment(ApartmentState.STA)]
        public void EasyTest(){
//            Process.Start(@"C:\Work\eXpandFramework\XafEasyTestInCodeNUnit-master\XafEasyTestInCodeNUnit-master\TestApplication.Win\bin\EasyTest\net462\TestApplication.Win.exe");
//            return;
            var winAdapter = new WinAdapter();
            var testApplication = new DevExpress.EasyTest.Framework.TestApplication();
            var document = new XmlDocument();
            testApplication.AdditionalAttributes=Enumerable.Range(0, 2).Select(i => {
                XmlAttribute attribute;
                if (i==0){
                    attribute = document.CreateAttribute("FileName");
//                    attribute.Value = @"C:\Work\eXpandFramework\XafEasyTestInCodeNUnit-master\XafEasyTestInCodeNUnit-master\TestApplication.Win\bin\EasyTest\net462\TestApplication.Win.exe";
                    attribute.Value = $@"{AppDomain.CurrentDomain.ApplicationPath()}\TestApplication.Win.exe";
//                    attribute.Value = @"C:\Work\eXpandFramework\Packages\src\Tests\ALL\Win\bin\Debug\ALL.Win.Tests.exe";
                }
                else{
                    attribute = document.CreateAttribute("CommunicationPort");
                    attribute.Value = "4100";
                }
                return attribute;
            }).ToArray();
            
            winAdapter.RunApplication(testApplication, $"ConnectionString={InMemoryDataStoreProvider.ConnectionString}");
            var commandAdapter = ((IApplicationAdapter)winAdapter).CreateCommandAdapter();

            winAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);

        }

    }
}