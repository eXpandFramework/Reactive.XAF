using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using JetBrains.Annotations;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using IDisposable = System.IDisposable;

namespace Xpand.TestsLib{
    public abstract class BaseTest : IDisposable{
        public const int LongTimeout = 500000;
        protected Platform GetPlatform(string platformName){
            return (Platform) Enum.Parse(typeof(Platform), platformName);
        }

        protected TimeSpan Timeout = TimeSpan.FromSeconds(Debugger.IsAttached ? 120 : 5);

        static BaseTest(){
            TextListener = new TextWriterTraceListener($@"{AppDomain.CurrentDomain.ApplicationPath()}\reactive.log");
            var traceSourceSwitch = new SourceSwitch("SourceSwitch", "Verbose");
            TraceSource = new TraceSource(nameof(BaseTest)){Switch = traceSourceSwitch};
            TraceSource.Listeners.Add(TextListener);
        }
        public static IEnumerable<Platform> PlatformDatasource(){
            yield return Platform.Web;
            yield return Platform.Win;
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

        [PublicAPI]
        protected static object[] ReactiveModules(){
            return AgnosticModules().Concat(WinModules()).Concat(WebModules()).OfType<ReactiveModuleBase>().Cast<object>().ToArray();
        }
        
        [PublicAPI]
        protected void WriteLine(bool value){
            TestContext.WriteLine(value);
        }

        [PublicAPI]
        protected void WriteLine(char value){
            TestContext.WriteLine(value);
        }

        [PublicAPI]
        protected void WriteLine(string value){
            WriteLine(value.ToCharArray());
        }

        protected void WriteLine(char[] value){
            TestContext.WriteLine(value);
        }

        [PublicAPI]
        protected void WriteLine(decimal value){
            TestContext.WriteLine(value);
        }

        public static TextWriterTraceListener TextListener{ get; }

        public static TraceSource TraceSource{ get; }
        

        public const string NotImplemented = "NotImplemented";

        [SetUp]
        public void Setup(){
            ReactiveLoggerService.RXLoggerLogPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                $"{TestContext.CurrentContext.Test.MethodName}_{TestContext.CurrentContext.Test.ID}_RXLogger{TestContext.CurrentContext.CurrentRepeatCount}.log");
        }

        [TearDown]
        public void Dispose(){
            XpoTypesInfoHelper.Reset();
            XafTypesInfo.HardReset();
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed){
                if (File.Exists(ReactiveLoggerService.RXLoggerLogPath)){
                    TestContext.AddTestAttachment(ReactiveLoggerService.RXLoggerLogPath);
                }
            }
        }
    }
}