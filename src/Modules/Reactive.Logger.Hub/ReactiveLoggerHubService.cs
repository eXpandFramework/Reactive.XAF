using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Grpc.Core;
using MagicOnion.Client;
using MagicOnion.Server;
using MessagePack;
using MessagePack.Resolvers;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using ListView = DevExpress.ExpressApp.ListView;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub{
    public static class ReactiveLoggerHubService{
        static readonly TraceEventReceiver Receiver = new TraceEventReceiver();
        

        internal static IObservable<Unit> Connect(this XafApplication application){
            MessagePackSerializer.SetDefaultResolver(ContractlessStandardResolver.Instance);
            var startServer = ReactiveLoggerService.RegisteredListener.StartServer(application).Replay(1).RefCount();
            var client = application.ConnectClient().Replay(1).RefCount();
            CleanUpHubResources(application, client, startServer);
            
            return startServer.ToUnit()
                .Merge(client.ToUnit())
                .Merge(application.WhenViewOnFrame().FirstAsync().SelectMany(frame => application.SaveServerTraceMessages().LoadTracesToListView(frame)));
            
        }

        public static void CleanUpHubResources(XafApplication application, IObservable<ITraceEventHub> client, IObservable<Server> startServer){
            application.WhenDisposed()
                .Select(_ => _.component is ILoggerHubClientApplication
                        ? client.SelectMany(hub => hub.DisposeAsync().ToObservable())
                        : startServer.SelectMany(server => server.ShutdownAsync().ToObservable().Select(unit => unit)))
                .Concat()
                .FirstAsync()
                .TraceRXLoggerHub()
                .Subscribe();
        }

        private static IObservable<Unit> LoadTracesToListView(this IObservable<TraceEvent[]> source,Frame frame){
            
            var synchronizationContext = SynchronizationContext.Current;
            return source
                .TakeUntil(frame.WhenDisposingFrame())
                .Throttle(TimeSpan.FromSeconds(1))
                .ObserveOn(synchronizationContext)
                .Do(events => {
                    if (events.Any()){
                        var listView = (ListView) frame?.View;
                        listView?.RefreshDataSource();
                    }
                }).ToUnit();

        }

        private static IObservable<TraceEvent[]> SaveServerTraceMessages(this XafApplication application){
            

            return application.BufferUntilCompatibilityChecked(TraceEventReceiver.TraceEvent)
                .Buffer(TimeSpan.FromSeconds(2)).WhenNotEmpty()
                .Select(list => {
                    return application.ObjectSpaceProvider.ToObjectSpace()
                        .SelectMany(space => space.SaveTraceEvent(list)).ToEnumerable().ToArray();
                });
        }


        internal static IObservable<TSource> TraceRXLoggerHub<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0){

            return source.Trace(name, ReactiveLoggerHubModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }

        private static IObservable<Server> StartServer(this IObservable<ReactiveTraceListener> source, XafApplication application){
            return source.SelectMany(listener => application is ILoggerHubClientApplication
                ? Observable.Empty<Server>()
                : application.ServerPortsList().FirstAsync()
                    .Select(modelServerPort => modelServerPort.ToServerPort().StartServer())
                    .TraceRXLoggerHub().Select(server => server));
//            return application.WhenCompatibilityChecked().SelectMany(_ => application is ILoggerHubClientApplication
//                ? Observable.Empty<Server>()
//                : application.ServerPortsList().DoNotComplete().FirstAsync()
//                    .Select(modelServerPort => modelServerPort.ToServerPort().StartServer())
//                    .TraceRXLoggerHub().Select(server => server));
        }

        private static IObservable<ITraceEventHub> ConnectClient(this XafApplication application){
//            return application.WhenWindowCreated()
//                .Select(window => window)
//                .When(TemplateContext.ApplicationWindow)
//                .Select(window => window)
//                .TemplateChanged()
return application.WhenCompatibilityChecked()
                .SelectMany(window => {
                    var loggerHub = application is ILoggerHubClientApplication
                        ? application.ServerPortsList()
                            .SelectMany(port => port.ConnectClient())
                        : Observable.Empty<ITraceEventHub>();
                    return loggerHub;
                })
                ;
        }

        public static IObservable<ITraceEventHub> ConnectClient(this IModelServerPort modelServerPort){
            return modelServerPort.DetectOnlineHub()
                .SelectMany(unit => {
                    var newClient = modelServerPort.ToServerPort().NewClient(Receiver);
                    return newClient.ConnectAsync().ToObservable()
//                            .Catch<Unit,Exception>(exception => Observable.Empty<Unit>())
                        .Merge(Unit.Default.AsObservable())
                            .Select(hub => newClient)
                        .DoAfterTerminate(() => {})
                        .DoOnDispose(() => { })
                        ;
                })
//                .Catch<ITraceEventHub,Exception>(exception => Observable.Empty<ITraceEventHub>())
                .Retry()

                .TraceRXLoggerHub();
        }

        public static bool PortInUse(int port){
            var inUse = false;
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var ipEndPoints = ipProperties.GetActiveTcpListeners();
            foreach (var endPoint in ipEndPoints)
                if (endPoint.Port == port){
                    inUse = true;
                    break;
                }
            return inUse;
        }

        public static bool PortInUse(this IModelServerPort modelServerPort){
            return PortInUse(modelServerPort.Port);
        }

        public static IObservable<Unit> DetectOffLineHub(this IModelServerPort modelServerPort){
            return Observable.Defer(() => Observable.Start(() => Observable.While(modelServerPort.PortInUse,Observable.Empty<Unit>())).Merge())
                    .Concat(Unit.Default.AsObservable()).FirstAsync()
                    .Do(unit => {
//                        socket.Disconnect(true);
//                        socket.Dispose();
                    })
                    .Select(unit => unit)
                    .TraceRXLoggerHub()
                
                ;

        }

        
        public static IObservable<IModelServerPort> DetectOnlineHub(this IModelServerPort modelServerPort){
            return Observable.Defer(() => Observable.Start(() => Observable.While(() => !modelServerPort.PortInUse(),Observable.Empty<Unit>())).Merge())
                    .Select(unit => unit)
                    .Concat(Unit.Default.AsObservable()).FirstAsync()
                    .RepeatWhen(ob =>ob.SelectMany(o => modelServerPort.DetectOffLineHub().Select(unit => unit)))
                    .Select(unit => modelServerPort)
                    .TraceRXLoggerHub()
                    .Finally(() => { })
                
                ;
        }

        public static IObservable<IModelServerPort> ServerPortsList(this XafApplication application){
            return application.ToReactiveModule<IModelReactiveModuleLogger>().Select(logger => logger.ReactiveLogger).Cast<IModelServerPorts>()
                .Select(logger => logger).Cast<IModelServerPorts>()
                .SelectMany(ports => ports.Ports);
        }

        public static Server StartServer(this ServerPort serverPort){
            
            var options = new MagicOnionOptions{IsReturnExceptionStackTraceInErrorDetail = true};
            var service = MagicOnionEngine.BuildServerServiceDefinition(new[]{typeof(ReactiveLoggerHubService).GetTypeInfo().Assembly},options);
            var server = new Server{
                Services = {service.ServerServiceDefinition},
                Ports = {serverPort}
            };

            server.Start();
            return server;
        }

        public static ServerPort ToServerPort(this IModelServerPort port){
            return new ServerPort(port.Host, port.Port, ServerCredentials.Insecure);
        }


        public static ITraceEventHub NewClient(this ServerPort serverPort,TraceEventReceiver receiver){
            var defaultChannel = new Channel(serverPort.Host, serverPort.Port, ChannelCredentials.Insecure);
            return StreamingHubClient.Connect<ITraceEventHub, ITraceEventHubReceiver>(defaultChannel,receiver);
        }

    }
}
