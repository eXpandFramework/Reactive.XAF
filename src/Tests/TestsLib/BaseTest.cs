using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Xpand.Extensions.AppDomain;
using Xpand.Extensions.Linq;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive;
using IDisposable = System.IDisposable;

namespace Xpand.TestsLib{
    public abstract class BaseTest : IDisposable{
        public const int LongTimeout = 500000;
        protected Platform GetPlatform(string platformName){
            return (Platform) Enum.Parse(typeof(Platform), platformName);
        }

        protected TimeSpan Timeout = TimeSpan.FromSeconds(Debugger.IsAttached ? 120 : 25);

        static BaseTest(){
            TextListener = new TextWriterTraceListener($@"{AppDomain.CurrentDomain.ApplicationPath()}\reactive.log");
            var traceSourceSwitch = new SourceSwitch("SourceSwitch", "Verbose");
            TraceSource = new TraceSource(nameof(BaseTest)){Switch = traceSourceSwitch};
            TraceSource.Listeners.Add(TextListener);
        }

        protected static object[] AgnosticModules(){
            return GetModules("Xpand.XAF.Modules*.dll","Core").Where(o => {
                var name = ((Type) o).Assembly.GetName().Name;
                return !name.EndsWith(".Win") && !name.EndsWith(".Web") && !name.EndsWith(".Tests");
            }).ToArray();
        }

        protected static object[] WinModules(){
            return GetModules("Xpand.XAF.Modules*.dll","Win");
        }

        protected static object[] WebModules(){
            return GetModules("Xpand.XAF.Modules*.dll","Web");
        }

        private static object[] GetModules(string pattern,string platform){
            return Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), pattern)
                .Where(s => !s.Contains(".Tests."))
                .Select(s => {
                    var assembly = Assembly.LoadFile(s);
                    return assembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(_ => _.Key == "Platform")
                        .Value == platform ? assembly.GetTypes().First(type =>
                            !type.IsAbstract && typeof(ModuleBase).IsAssignableFrom(type)) : null;
                })
                .WhereNotDefault()
                .Cast<object>().ToArray();
        }

        protected static object[] ReactiveModules(){
            return AgnosticModules().Concat(WinModules()).Concat(WebModules()).OfType<ReactiveModuleBase>().Cast<object>().ToArray();
        }

        protected void WriteLine(bool value){
            TestContext.WriteLine(value);
        }

        protected void WriteLine(char value){
            TestContext.WriteLine(value);
        }

        protected void WriteLine(string value){
            WriteLine(value.ToCharArray());
        }

        protected void WriteLine(char[] value){
            TestContext.WriteLine(value);
        }

        protected void WriteLine(decimal value){
            TestContext.WriteLine(value);
        }

        public static TextWriterTraceListener TextListener{ get; }

        public static TraceSource TraceSource{ get; }

        public const string NotImplemented = "NotImplemented";

        [TearDown]
        public void Dispose(){
            XpoTypesInfoHelper.Reset();
            XafTypesInfo.HardReset();
//            GC.Collect();
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed){
//                var settingsPath = $"{AppDomain.CurrentDomain.ApplicationPath()}\\TestRun.Settings";
//                while (!File.Exists(settingsPath)){
//                    var parent = new DirectoryInfo($"{Path.GetDirectoryName(settingsPath)}").Parent;
//                    if (parent == null){
//                        settingsPath = null;
//                        break;
//                    }
//                    settingsPath = $"{parent.FullName}\\TestRun.Settings";
//                }
//
//                if (settingsPath != null){
//                    var settings=Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(settingsPath);
//                    var fileName = $"{settings.TestArtifactsDirectory}\\TestsRun.zip";
//                    ZipFile.CreateFromDirectory(settings.TestArtifactsDirectory,fileName,CompressionLevel.NoCompression, false);
//                    TestContext.AddTestAttachment(fileName);
//                }    
            }
        }
    }
}