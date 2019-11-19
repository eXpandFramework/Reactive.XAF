using System.Linq;
using System.Xml;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp.Xpo;
using Xpand.Extensions.Linq;

namespace Xpand.TestsLib.EasyTest{
    public static class WinAdapterExtesions{
        public static ICommandAdapter CreateCommandAdapter(this IApplicationAdapter winAdapter){
            return winAdapter.CreateCommandAdapter();
        }

        public static void AddAttribute(this TestApplication testApplication, string name, string value){
            var document = new XmlDocument();
            var attribute = document.CreateAttribute(name);
            attribute.Value = value;
            testApplication.AdditionalAttributes = testApplication.AdditionalAttributes.Add(attribute).ToArray();
        }

        public static TestApplication RunWebApplication(this IApplicationAdapter winAdapter, string physicalPath,
            string port){
            var testApplication = new TestApplication();
            testApplication.AddAttribute("SingleWebDev", "true");
            testApplication.AddAttribute("DontRestartIIS", "true");
            testApplication.AddAttribute("UseIISExpress", "true");
            testApplication.AddAttribute("PhysicalPath", physicalPath);
            testApplication.AddAttribute("URL", $"http://localhost:{port}/default.aspx");
            winAdapter.RunApplication(testApplication);
            return testApplication;
        }

        public static TestApplication RunWinApplication(this IApplicationAdapter winAdapter, string fileName,
            int port = 4100){
            var testApplication = new TestApplication();
            testApplication.AddAttribute("FileName", fileName);
            testApplication.AddAttribute("CommunicationPort", port.ToString());
            winAdapter.RunApplication(testApplication);
            return testApplication;
        }

        private static void RunApplication(this IApplicationAdapter winAdapter, TestApplication testApplication){
            winAdapter.RunApplication(testApplication,
                $"ConnectionString={InMemoryDataStoreProvider.ConnectionString}");
        }
    }
}