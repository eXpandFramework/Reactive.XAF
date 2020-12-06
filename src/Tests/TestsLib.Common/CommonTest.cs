#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using JetBrains.Annotations;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using AssemblyExtensions = Xpand.Extensions.AssemblyExtensions.AssemblyExtensions;
using IDisposable = System.IDisposable;

namespace Xpand.TestsLib.Common{
    public abstract class CommonTest : IDisposable{
        public const int LongTimeout = 900000;
        [UsedImplicitly]
        protected Platform GetPlatform(string platformName) => (Platform) Enum.Parse(typeof(Platform), platformName);

        [UsedImplicitly] protected TimeSpan Timeout = TimeSpan.FromSeconds(Debugger.IsAttached ? 120 : 5);

        protected CommonTest() => AssemblyExtensions.EntryAssembly=GetType().Assembly;

        static CommonTest() {
            TextListener = new TextWriterTraceListener($@"{AppDomain.CurrentDomain.ApplicationPath()}\reactive.log");
            var traceSourceSwitch = new SourceSwitch("SourceSwitch", "Verbose");
            TraceSource = new TraceSource(nameof(CommonTest)){Switch = traceSourceSwitch};
            TraceSource.Listeners.Add(TextListener);
        }
        [UsedImplicitly]
        public static IEnumerable<Platform> PlatformDataSource(){
            yield return Platform.Web;
            yield return Platform.Win;
        }

        protected static object[] AgnosticModules() 
            => GetModules("Xpand.XAF.Modules*.dll","Core").Where(o => {
                var name = ((Type) o).Assembly.GetName().Name;
                return !name.EndsWith(".Win") && !name.EndsWith(".Web") && !name.EndsWith(".Tests");
            }).ToArray();

        protected static object[] WinModules() 
            => GetModules("Xpand.XAF.Modules*.dll","Win");

        protected static object[] WebModules() 
            => GetModules("Xpand.XAF.Modules*.dll","Web");

        private static object[] GetModules(string pattern,string platform){
            return Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), pattern)
                .Where(s => !s.Contains(".Tests."))
                .Where(s => {
#if XAF191
                    return !s.Contains("DocumentStyleManager");
#else
                return true;
#endif
                })
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
        protected static object[] ReactiveModules() 
            => AgnosticModules().Concat(WinModules()).Concat(WebModules()).OfType<ReactiveModuleBase>().Cast<object>().ToArray();

        [PublicAPI]
        protected void WriteLine(bool value) => TestContext.WriteLine(value);

        [PublicAPI]
        protected void WriteLine(char value) => TestContext.WriteLine(value);

        [PublicAPI]
        protected void WriteLine(string value) => WriteLine(value.ToCharArray());

        protected void WriteLine(char[] value) => TestContext.WriteLine(value);

        [PublicAPI]
        protected void WriteLine(decimal value) => TestContext.WriteLine(value);

        protected readonly List<string> LogPaths=new List<string>(){ReactiveLoggerService.RXLoggerLogPath,Path.Combine(TestContext.CurrentContext.TestDirectory,"expressappframework.log")};
        
        public static TextWriterTraceListener TextListener{ get; }

        public static TraceSource TraceSource{ get; }
        

        [UsedImplicitly] public const string NotImplemented = "NotImplemented";

        [SetUp]
        public virtual void Setup() 
            => ReactiveLoggerService.RXLoggerLogPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                $"{TestContext.CurrentContext.Test.MethodName}_{TestContext.CurrentContext.Test.ID}_RXLogger{TestContext.CurrentContext.CurrentRepeatCount}.log");

        [TearDown]
        public virtual void Dispose(){
            XafTypesInfo.HardReset();
            // XpoTypesInfoHelper.Reset();
            
            // var typesInfo = ((TypesInfo) XafTypesInfo.Instance);
            // var entityStores = ((IList<IEntityStore>) typesInfo.GetFieldValue("entityStores"));
            // entityStores.Remove(store => !(store is NonPersistentTypeInfoSource));
            

            try{
                var text = GetLogText();
                if (!string.IsNullOrEmpty(text)){
                    var zipPPath = Path.Combine(TestContext.CurrentContext.TestDirectory,$"{GetTestName()}.gz");
                    File.WriteAllBytes(zipPPath,text.GZip());
                    TestContext.AddTestAttachment(zipPPath);    
                }
            }
            catch (Exception e){
                TestContext.Out.Write(e);
            }
            
        }

        private static string GetTestName() 
            => $"{TestContext.CurrentContext.Test.MethodName}{TestContext.CurrentContext.Test.Arguments.Select(o => $"{o}").Join("_")}";

        private string GetLogText() 
            => LogPaths.Select(logPath => {
                if (File.Exists(logPath)){
                    var tempFileName = Path.GetTempFileName();
                    File.Copy(logPath!, tempFileName, true);
                    return $"{File.ReadAllText(tempFileName)}{Environment.NewLine}";
                }
                return null;
            }).Join(Environment.NewLine).Trim();
    }
}