using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.AppDomain;
using Xpand.Extensions.String;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ProgressBarViewItem{
    public interface IModelProgressBarViewItem : IModelViewItem {
    }

    
    public abstract class ProgressBarViewItemBase:ViewItem,IComplexViewItem,IObserver<decimal>{
        readonly Subject<Unit> _breakLinksToControl=new Subject<Unit>();
        private static Type _progressBarControlType;
        readonly Subject<decimal> _positionSubject=new Subject<decimal>();
        private static Platform? _platform;
        private static MethodInvoker _percentage;
        private XafApplication _application;

        private static void Init(Platform platform){
            if (!_platform.HasValue){
                _platform = platform;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                if (_platform == Platform.Win){
                    var assemmbly = assemblies
                        .FirstOrDefault(assembly => assembly.FullName.StartsWith("DevExpress.XtraEditors"));
                    _progressBarControlType = assemmbly?.GetType("DevExpress.XtraEditors.ProgressBarControl");
                }
                else if (_platform == Platform.Web){
                    var assemmbly = assemblies
                        .FirstOrDefault(assembly => assembly.FullName.StartsWith("DevExpress.Web.v"));
                    _progressBarControlType = assemmbly?.GetType("DevExpress.Web.ASPxProgressBar");

                    _percentage = AppDomain.CurrentDomain.AssemblySystemWeb().TypeUnit().Percentage();
                    var assemblyDevExpressExpressAppWeb = AppDomain.CurrentDomain.AssemblyDevExpressExpressAppWeb();
                    _asssignClientHanderSafe = assemblyDevExpressExpressAppWeb.TypeClientSideEventsHelper().AsssignClientHanderSafe();

                
                    var methodInfoGetShowMessageScript = assemblyDevExpressExpressAppWeb.GetType("DevExpress.ExpressApp.Web.PopupWindowManager").GetMethod("GetShowMessageScript",BindingFlags.Static|BindingFlags.NonPublic);
                    _delegateForGetShowMessageScript = methodInfoGetShowMessageScript.DelegateForCallMethod();
                }
            }
        }

        protected ProgressBarViewItemBase(IModelProgressBarViewItem info, Type classType)
            : base(classType, info.Id){
            PollingInterval = 1000;
        }

        [PublicAPI]
        public void ProcessAction(string parameter){
            var script = $"{parameter}.SetPosition('{Position}');";
            if (FinishOptions!=null) {
                script = $"{parameter}.SetPosition(100);{_delegateForGetShowMessageScript(null, FinishOptions)}";
                SetFinishOptions(null);
            }
            _application.MainWindow.CallMethod("RegisterStartupScript", _handlerId, script, true);
        }
        public MessageOptions FinishOptions { get; private set; }

        public virtual void SetFinishOptions(MessageOptions messageOptions) {
            var finishOptions = messageOptions;
            FinishOptions = finishOptions;
            if (_platform == Platform.Win){
                _application.ShowViewStrategy.ShowMessage(messageOptions);
            }
        }

        [PublicAPI]
        public virtual void Start(SynchronizationContext synchronizationContext=null){
            synchronizationContext ??= SynchronizationContext.Current;
            if (synchronizationContext == null){
                throw new ArgumentNullException(nameof(synchronizationContext));
            }
            if (_platform == Platform.Web){
                _asssignClientHanderSafe(null,Control,"Init", GetInitScript(), "grid.Init");
            }
            _positionSubject
                .ObserveOn(synchronizationContext)
                .Do(SetPosition)
                .Finally(() => SetPosition(0))
                .TakeUntil((_platform==Platform.Win?_breakLinksToControl.AsObservable():Observable.Empty<Unit>())) 
                .Subscribe();
            FinishOptions = null;
        }

        private string GetInitScript(){
            var script = _callBackManager.CallMethod("GetScript", _handlerId, $"'{_clientInstancename}'", "", false);
            return $@"function(s,e) {{
                    s.timer = window.setInterval(function(){{
                                if (s.GetPosition()==100){{
                                    window.clearInterval(s.timer);
                                    s.SetPosition(0);
                                    return;
                                }}
                                var previous = startProgress;
console.log('p='+previous);
                                startProgress = function () {{ }}; 
                                {script};
                                startProgress = previous;
                            }},{PollingInterval});
                }}";
        }

        public override void BreakLinksToControl(bool unwireEventsOnly){
            _registerHandlerSubscription?.Dispose();
            _breakLinksToControl.OnNext(Unit.Default);
            base.BreakLinksToControl(unwireEventsOnly);
        }

        public decimal Position { get; private set; }
        
        public void SetPosition(decimal value){
            Position = value;
            if (_platform == Platform.Win){
                Control.SetPropertyValue("Position", (int)value);
            }
        }

        [PublicAPI]
        public int PollingInterval{ get; set; }
        string _handlerId; 
        private static MethodInvoker _asssignClientHanderSafe;
        private object _callBackManager;
        private string _clientInstancename;
        private static MethodInvoker _delegateForGetShowMessageScript;
        private IDisposable _registerHandlerSubscription;

        protected override object CreateControlCore(){
            var instance = _progressBarControlType.CreateInstance();
            if (_platform == Platform.Web){
                instance.SetPropertyValue("ClientInstanceName", _clientInstancename);
                instance.SetPropertyValue("Width", _percentage(null, 100d));
                _registerHandlerSubscription = View.WhenControlsCreated()
                    .FirstAsync()
                    .Do(_ => {
                        _clientInstancename = Id.CleanCodeName();
                        _handlerId = $"{GetType().FullName}{_clientInstancename}";
                        _callBackManager = _application.MainWindow.Template.GetPropertyValue("CallbackManager");
                        _callBackManager.CallMethod("RegisterHandler", _handlerId, this);
                    })
                    .TraceProgressBarViewItemModule(compositeView => compositeView.Id)
                    .Subscribe();
            }
            return instance;
        }

        public void Setup(IObjectSpace objectSpace, XafApplication application){
            _application = application;
            var platform = application.Modules.GetPlatform();
            Init(platform);
        }

        public void OnNext(decimal value){
            _positionSubject.OnNext(value);
        }

        public void OnError(Exception error){
            
        }

        public void OnCompleted(){
            _positionSubject.OnCompleted();
        }

    }
}