using System;
using System.Diagnostics;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using Xpand.Source.Extensions.System.AppDomain;
using Xunit.Abstractions;
using IDisposable = System.IDisposable;

namespace TestsLib{
    public abstract class BaseTest : IDisposable{
        
        protected TimeSpan Timeout = TimeSpan.FromSeconds(Debugger.IsAttached?120:5);

        static BaseTest(){
            TextListener = new TextWriterTraceListener($@"{AppDomain.CurrentDomain.ApplicationPath()}\reactive.log");
            var traceSourceSwitch = new SourceSwitch("SourceSwitch", "Verbose");
            TraceSource = new TraceSource(nameof(BaseTest)){Switch = traceSourceSwitch};
            TraceSource.Listeners.Add(TextListener);
            
        }

        protected BaseTest(){
        }

        public static TextWriterTraceListener TextListener{ get; }

        public static TraceSource TraceSource{ get; }

        public const string NotImplemented = "NotImplemented";
        protected BaseTest(ITestOutputHelper output){
            Output = output;
        }

        public ITestOutputHelper Output{ get; }

        public virtual void Dispose(){
            XpoTypesInfoHelper.Reset();
            XafTypesInfo.HardReset();
        }
    }
}