using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using MagicOnion.Server.Hubs;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub{
    public class TraceEventHub : StreamingHubBase<ITraceEventHub, ITraceEventHubReceiver>,ITraceEventHub{
        static readonly ISubject<Unit> ConnectingSubject=Subject.Synchronize(new Subject<Unit>());
        private IGroup _group;

        public static IObservable<Unit> Connecting => Observable.AsObservable(ConnectingSubject);

        public  Task ConnectAsync(){
            ConnectingSubject.OnNext(Unit.Default);
            return ReactiveLoggerService.ListenerEvents
                .Select(_ => {
                    Broadcast(_group).OnTraceEvent((TraceEventMessage) _);
                    return Unit.Default;
                })
                .DoNotComplete()
                .ToTask();

        }

        protected override async ValueTask OnConnecting(){
            _group = await Group.AddAsync("global");
            
        }

        protected override async ValueTask OnDisconnected(){
            if (_group != null) await _group.RemoveAsync(Context);
        }
    }
}