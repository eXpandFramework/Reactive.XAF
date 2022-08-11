using System.Threading.Tasks;
using MagicOnion;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub{
    public interface ITraceEventHub : IStreamingHub<ITraceEventHub, ITraceEventHubReceiver>{
        Task ConnectAsync();
    }

    public interface ITraceEventHubReceiver{
        void OnTraceEvent(TraceEventMessage traceEventMessage);
    }

    public interface ILoggerHubClientApplication{
    }
}