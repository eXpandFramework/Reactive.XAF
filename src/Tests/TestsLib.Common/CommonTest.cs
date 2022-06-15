#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.ExpressApp;

using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.Threading;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using AssemblyExtensions = Xpand.Extensions.AssemblyExtensions.AssemblyExtensions;
using IDisposable = System.IDisposable;
using TestScheduler = akarnokd.reactive_extensions.TestScheduler;

namespace Xpand.TestsLib.Common{
    public abstract class CommonTest:ReactiveTest, IDisposable{
        public readonly TestScheduler TestScheduler=new();
        public const int LongTimeout = 900000;
        
        protected Platform GetPlatform(string platformName) => (Platform) Enum.Parse(typeof(Platform), platformName);

         protected TimeSpan Timeout = TimeSpan.FromSeconds(Debugger.IsAttached ? 120 : 5);

        protected CommonTest() => AssemblyExtensions.EntryAssembly=GetType().Assembly;
        public static string EasyTestTraceLevel = "Verbose";
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool Get(string name,ref object __result) {
            if (name == nameof(EasyTestTraceLevel)) {
                __result = EasyTestTraceLevel;
                return false;
            }
            return true;
        }
        static CommonTest() {
            TextListener = new TextWriterTraceListener($@"{AppDomain.CurrentDomain.ApplicationPath()}\reactive.log");
            var traceSourceSwitch = new SourceSwitch("SourceSwitch", "Verbose");
            TraceSource = new TraceSource(nameof(CommonTest)){Switch = traceSourceSwitch};
            TraceSource.Listeners.Add(TextListener);
            Trace.Listeners.Add(new TextWriterTraceListener($@"{AppDomain.CurrentDomain.ApplicationPath()}\easytest.log"));
        }
        
        public static IEnumerable<Platform> PlatformDataSource(){
            yield return Platform.Web;
            yield return Platform.Win;
        }

        [OneTimeSetUp]
        public virtual void Init() {
            TestScheduler.AdvanceTimeBy((long)DateTimeOffset.Now.ToUniversalTime().Subtract(TestScheduler.Now).TotalMilliseconds);
        }

        [OneTimeTearDown]
        public virtual void Cleanup() {
            
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
                .Where(_ => {
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

        
        protected static object[] ReactiveModules() 
            => AgnosticModules().Concat(WinModules()).Concat(WebModules()).OfType<ReactiveModuleBase>().Cast<object>().ToArray();

        
        protected void WriteLine(bool value) => TestContext.WriteLine(value);

        
        protected void WriteLine(char value) => TestContext.WriteLine(value);

        
        protected void WriteLine(string value) => WriteLine(value.ToCharArray());

        protected void WriteLine(char[] value) => TestContext.WriteLine(value);

        
        protected void WriteLine(decimal value) => TestContext.WriteLine(value);

        protected readonly List<string> LogPaths=new(){ReactiveLoggerService.RXLoggerLogPath,Path.Combine(TestContext.CurrentContext.TestDirectory,"expressappframework.log")};
        
        public static TextWriterTraceListener TextListener{ get; }

        public static TraceSource TraceSource{ get; }
        

         public const string NotImplemented = "NotImplemented";

        [SetUp]
        public virtual void Setup() {
            TestContext.Out.Write(TestContext.CurrentContext.Test.FullName);
        }

        [TearDown]
        public virtual void Dispose() {
            ResetXAF();

            // var typesInfo = ((TypesInfo) XafTypesInfo.Instance);
            // var entityStores = ((IList<IEntityStore>) typesInfo.GetFieldValue("entityStores"));
            // entityStores.Remove(store => !(store is NonPersistentTypeInfoSource));
            

            try{
                LogPaths.ForEach(path => {
                    if (File.Exists(path)) {
                        var tempFileName = Path.GetTempFileName();
                        File.Copy(path!, tempFileName, true);
                        var text = $"{File.ReadAllText(tempFileName)}{Environment.NewLine}";
                        var zipPPath = Path.Combine(TestContext.CurrentContext.TestDirectory,$"{Path.GetFileNameWithoutExtension(path)}.gz");
                        File.WriteAllBytes(zipPPath,text.GZip());
                        TestContext.AddTestAttachment(zipPPath);   
                    }
                });
            }
            catch (Exception e){
                TestContext.Out.Write(e);
            }
        }
        static readonly object Locker=new();
        protected virtual void ResetXAF() {
            lock (Locker) {
                XafTypesInfo.HardReset();
            }
            // XpoTypesInfoHelper.Reset();
        }

        protected void Await(Func<Task> invoker) {
            SingleThreadedSynchronizationContext.Await(invoker);
        }
        protected void Await(Func<IObservable<object>> invoker) {
            SingleThreadedSynchronizationContext.Await(() => invoker().ToTask());
        }
        
    }

}