using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Runtime.ExceptionServices;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Logger.Hub;

namespace Xpand.TestsLib{
    public class TestWinApplication : WinApplication, ITestApplication{
        private readonly bool _transmitMessage;
        private readonly int _createdOnThread = Environment.CurrentManagedThreadId;
        private readonly System.Threading.SynchronizationContext _createdContext = System.Threading.SynchronizationContext.Current;
        private readonly int _liveSeq = RegisterLive();
        private static readonly TimeSpan OwnerTeardownTimeout = TimeSpan.FromSeconds(5);
        private static int _liveCount;
        private int _disposed;

        static TestWinApplication() {
            Control.CheckForIllegalCrossThreadCalls = true;
        }

        private static int RegisterLive() {
            var live = System.Threading.Interlocked.Increment(ref _liveCount);
            if (live > 1) {
                Console.Error.WriteLine($"[TestWinApplication] created while {live - 1} previous instance(s) still live; process threads={Process.GetCurrentProcess().Threads.Count}");
            }
            return live;
        }

        public TestWinApplication(Type sutModule, bool transmitMessage = true, bool handleExceptions=true) {
            // SettingUp += (_, args) => ((ExportedTypeCollection)args.SetupParameters.DomainComponents).Add(typeof(TraceEvent));
            // Modules.Add((ModuleBase)sutModule.CreateInstance());
            _transmitMessage = transmitMessage;
            SUTModule = sutModule;
            CustomHandleException += (_, e) => {
                if (handleExceptions){
                    Console.Error.WriteLine($"[TestWinApplication] UI-thread exception on managed thread {Environment.CurrentManagedThreadId}: {e.Exception}");
                    ExceptionDispatchInfo.Capture(e.Exception).Throw();
                }
            };
            // TraceClientConnected = this.ClientConnect();
            // TraceClientBroadcast = this.ClientBroadcast();
        }

        // protected override string GetModulesVersionInfoFilePath() {
        //     return null;
        // }

        public bool TransmitMessage => _transmitMessage;

        // public IObservable<Unit> TraceClientBroadcast{ get; set; }


        // public IObservable<Unit> TraceClientConnected{ get; set; }
        
        public Type SUTModule{ get; }

        protected override void Dispose(bool disposing){
            if (!disposing) {
                base.Dispose(false);
                return;
            }
            RunOnOwnerThread("disposed", () => {
                if (System.Threading.Interlocked.Exchange(ref _disposed, 1) == 1) {
                    return;
                }
                System.Threading.Interlocked.Decrement(ref _liveCount);
                if (_transmitMessage){
                    // TraceClientConnected.ToTaskWithoutConfigureAwait().GetAwaiter().GetResult();
                    // TraceClientBroadcast.ToTaskWithoutConfigureAwait().GetAwaiter().GetResult();
                }

                base.Dispose(true);
            });
        }

        /// <summary>
        /// Runs the teardown on the thread that created this application. DevExpress'
        /// dispose closes the main form, and WinForms refuses a control-handle touch
        /// from any thread but the one that owns it, so a test resuming on a pool
        /// thread after ConfigureAwait(false) closed LightStyleMainRibbonForm
        /// off-thread. An owner that cannot be reached in time is logged, never
        /// silently skipped and never waited on forever.
        /// </summary>
        private void RunOnOwnerThread(string what, Action teardown) {
            if (Environment.CurrentManagedThreadId == _createdOnThread) {
                teardown();
                return;
            }
            if (_createdContext == null) {
                LogThreadMismatch(what, "no captured context");
                teardown();
                return;
            }
            Exception error = null;
            var ran = new System.Threading.ManualResetEventSlim(false);
            _createdContext.Post(_ => {
                try {
                    teardown();
                }
                catch (Exception exception) {
                    error = exception;
                }
                finally {
                    ran.Set();
                }
            }, null);
            if (!ran.Wait(OwnerTeardownTimeout)) {
                LogThreadMismatch(what, $"owner did not run it within {OwnerTeardownTimeout.TotalSeconds}s");
                teardown();
                return;
            }
            if (error != null) {
                ExceptionDispatchInfo.Capture(error).Throw();
            }
        }

        private void LogThreadMismatch(string what, string reason)
            => Console.Error.WriteLine($"[TestWinApplication] {what} on managed thread {Environment.CurrentManagedThreadId} but created on {_createdOnThread}; {reason}, ran in place");

        readonly Subject<Form> _modelEditorForm = new();

        public TestWinApplication(){
        }

        public IObservable<Form> ModelEditorForm => _modelEditorForm.AsObservable();

        protected override Form CreateModelEditorForm(){
            var modelEditorForm = base.CreateModelEditorForm();
            _modelEditorForm.OnNext(modelEditorForm);
            return modelEditorForm;
        }

//        protected override LayoutManager CreateLayoutManagerCore(bool simple){
//            if (!simple){
//                var controlMock = new Mock<Control>(){CallBase = true};
//                var layoutManagerMock = new Mock<WinLayoutManager>(){CallBase = true};
//                layoutManagerMock.Setup(_ => _.LayoutControls(It.IsAny<IModelNode>(), It.IsAny<ViewItemsCollection>())).Returns(controlMock.Object);
//            
//                return layoutManagerMock.Object;
//            }
//
//            return new WinSimpleLayoutManager();
//        }

        protected override string GetModelCacheFileLocationPath() => null;

        protected override string GetDcAssemblyFilePath() => null;

        public override void StartSplash(){
            
        }

        protected override string GetModelAssemblyFilePath() => $@"{AppDomain.CurrentDomain.ApplicationPath()}\ModelAssembly{Guid.NewGuid()}.dll";
    }

    
    static class TestApplicationExtensions{
        public static IObservable<Unit> ClientBroadcast(this ITestApplication application) 
            => Process.GetProcessesByName("Xpand.XAF.Modules.Reactive.Logger.Client.Win").Any()
                ? TraceEventHub.Trace.TakeFirst(_ => _.Source == application.SUTModule.Name).ToUnit()
                    .SubscribeReplay()
                : Unit.Default.Observe();

        
        public static IObservable<Unit> ClientConnect(this ITestApplication application) 
            => Process.GetProcessesByName("Xpand.XAF.Modules.Reactive.Logger.Client.Win").Any()
                ? TraceEventHub.Connecting.TakeFirst().SubscribeReplay()
                : Unit.Default.Observe();
    }
}