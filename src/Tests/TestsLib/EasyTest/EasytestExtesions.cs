using System.Linq;
using System.Xml;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp.EasyTest.WebAdapter;
using DevExpress.ExpressApp.Xpo;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.TestsLib.EasyTest{
    public static class EasytestExtesions{
        

        public static bool IsWeb(this TestApplication application) {
            return application.AdditionalAttributes.Any(_ => _.Name == "URL");
        }

        public static T ConnvertTo<T>(this Command command) where T:Command{
            var t = (T)typeof(T).CreateInstance();
            t.Parameters.MainParameter = command.Parameters.MainParameter??new MainParameter();
            t.Parameters.ExtraParameter = command.Parameters.ExtraParameter??new MainParameter();
            t.SetPropertyValue("ExpectException",command.ExpectException);
            foreach (var parameter in command.Parameters){
                t.Parameters.Add(parameter);
            }
            return t;
        }


        public static TestApplication GetTestApplication(this ICommandAdapter adapter){
            return adapter is WebCommandAdapter ? (TestApplication) EasyTestWebApplication.Instance : EasyTestWinApplication.Instance;
        }

        public static void Execute(this ICommandAdapter adapter,params Command[] commands){
            foreach (var command in commands){
                if (command is IRequireApplicationOptions requireApplicationOptions){
                    requireApplicationOptions.SetApplicationOptions(adapter.GetTestApplication());
                }
                try{
                    command.Execute(adapter);
                }
                catch (CommandException){
                    if(!command.ExpectException) {
                        throw;
                    }

                }
            }
        }

        public static ICommandAdapter CreateCommandAdapter(this IApplicationAdapter adapter){
            return adapter.CreateCommandAdapter();
        }

        public static void AddAttribute(this TestApplication testApplication, string name, string value){
            var document = new XmlDocument();
            var attribute = document.CreateAttribute(name);
            attribute.Value = value;
            testApplication.AdditionalAttributes = testApplication.AdditionalAttributes.Add(attribute).ToArray();
        }

        public static TestApplication RunWebApplication(this IApplicationAdapter adapter, string physicalPath, int port){
            var testApplication = EasyTestWebApplication.New(physicalPath,port);
            adapter.RunApplication(testApplication);
            return testApplication;
        }

        public static TestApplication RunWinApplication(this IApplicationAdapter adapter, string fileName, int port = 4100){
            var testApplication = EasyTestWinApplication.New(fileName,port);
            adapter.RunApplication(testApplication);
            return testApplication;
        }

        private static void RunApplication(this IApplicationAdapter adapter, TestApplication testApplication){
            adapter.RunApplication(testApplication, $"ConnectionString={InMemoryDataStoreProvider.ConnectionString}");
        }
    }
}