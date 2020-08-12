using System.IO;
using System.Linq;
using System.Xml;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp.EasyTest.WebAdapter;
using DevExpress.ExpressApp.EasyTest.WinAdapter;
using Fasterflect;
using Newtonsoft.Json;
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

        public static string EasyTestSettingsFile(this TestApplication application){
            var path = application.AdditionalAttributes.FirstOrDefault(attribute => attribute.LocalName == "FileName")?.Value;
            path = path != null ? Path.GetDirectoryName(path) : application.AdditionalAttributes.First(attribute => attribute.LocalName=="PhysicalPath").Value;
            return $"{path}\\EasyTestSettings.json";
        }

        public static ICommandAdapter CreateCommandAdapter(this IApplicationAdapter adapter) => adapter.CreateCommandAdapter();

        public static void AddAttribute(this TestApplication testApplication, string name, string value){
            var document = new XmlDocument();
            var attribute = document.CreateAttribute(name);
            attribute.Value = value;
            testApplication.AdditionalAttributes = testApplication.AdditionalAttributes.Add(attribute).ToArray();
        }

        public static TestApplication RunWebApplication(this WebAdapter adapter, string physicalPath, int port,string connectionString){
            var testApplication = EasyTestWebApplication.New(physicalPath,port);
            testApplication.ConfigSettings(connectionString);
            adapter.RunApplication(testApplication, null);
            return testApplication;
        }

        public static TestApplication RunWinApplication(this WinAdapter adapter, string fileName,string connectionString){
            var testApplication = EasyTestWinApplication.New(fileName);
            testApplication.ConfigSettings(connectionString);
            adapter.RunApplication(testApplication, null);
            return testApplication;
        }

        private static void ConfigSettings(this TestApplication application,string connectionString){
            File.WriteAllText(application.EasyTestSettingsFile(),
                JsonConvert.SerializeObject(new{ConnectionString = connectionString}));
        }

        public static TestApplication RunWinApplication(this WinAdapter adapter, string fileName, int port = 4100){
            var testApplication = EasyTestWinApplication.New(fileName,port);
            adapter.RunApplication(testApplication, null);
            return testApplication;
        }
    }
}