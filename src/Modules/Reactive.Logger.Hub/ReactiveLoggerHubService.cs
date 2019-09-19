using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
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
            if (!(application is ILoggerHubClientApplication)){
                TraceEventHub.Init();
            }
            var startServer = Observable.Start(application.StartServer).Merge().Publish().RefCount();
            var client = Observable.Start(application.ConnectClient).Merge().Publish().RefCount();

            CleanUpHubResources(application, client, startServer);
            
            
            var saveServerTraceMessages = application.SaveServerTraceMessages().Publish().RefCount();
            return startServer.ToUnit()
                .Merge(client.ToUnit())
                .Merge(saveServerTraceMessages.ToUnit())
                .Merge(application.WhenViewOnFrame(typeof(TraceEvent))
                    .SelectMany(frame => saveServerTraceMessages.LoadTracesToListView(frame)))
                ;
            
        }

        public static void CleanUpHubResources(XafApplication application, IObservable<ITraceEventHub> client, IObservable<Server> startServer){
            application.WhenDisposed().Zip(startServer, (tuple, server) => server.ShutdownAsync().ToObservable().Select(unit => unit))
                .Concat()
                .FirstOrDefaultAsync()
                .Subscribe();
        }

        private static IObservable<Unit> LoadTracesToListView(this IObservable<TraceEvent[]> source,Frame frame){
            var synchronizationContext = SynchronizationContext.Current;
            return source.Select(_ => _)
//                .TakeUntil(frame.WhenDisposingFrame())
//                .Throttle(TimeSpan.FromSeconds(1))
                .ObserveOn(synchronizationContext)
                .Select(events => {
                    if (events.Any()){
                        var listView = (ListView) frame?.View;
                        listView?.RefreshDataSource();
                    }

                    return events;
                }).ToUnit();

        }

        private static IObservable<TraceEvent[]> SaveServerTraceMessages(this XafApplication application){
            return application.BufferUntilCompatibilityChecked(TraceEventReceiver.TraceEvent)
                .Buffer(TimeSpan.FromSeconds(2)).WhenNotEmpty()
                .TakeUntil(application.WhenDisposed())
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

        private static IObservable<Server> StartServer(this  XafApplication application){
            return application is ILoggerHubClientApplication
                ? Observable.Empty<Server>()
                : application.ServerPortsList().FirstAsync()
                    .Select(modelServerPort => modelServerPort.ToServerPort().StartServer())
                    .TraceRXLoggerHub().Select(server => server);
        }

        private static IObservable<ITraceEventHub> ConnectClient(this XafApplication application){
            return application.WhenCompatibilityChecked()
                .SelectMany(window => {
                    var loggerHub = application is ILoggerHubClientApplication
                        ? application.ClientPortsList().ToObservable(Scheduler.Default)
                            .SelectMany(port => port.ConnectClient()
                                .TakeUntil(application.WhenDisposed()))
                        : Observable.Empty<ITraceEventHub>();
                    return loggerHub;
                })
                ;
        }

        public static IObservable<ITraceEventHub> ConnectClient(this (string host,int port) tuple){
            return tuple.DetectOnlineHub()
                .SelectMany(_ => {
                    var newClient = _.ToServerPort().NewClient(Receiver);
                    return newClient.ConnectAsync().ToObservable()
//                            .Catch<Unit,Exception>(exception => Observable.Empty<Unit>())
                        .Merge(Unit.Default.AsObservable())
                        .Select(hub => tuple)
                        .TraceRXLoggerHub().To(newClient)
                        ;
                })
//                .Catch<ITraceEventHub,Exception>(exception => Observable.Empty<ITraceEventHub>())
                .Retry();
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

        public static bool PortInUse(this (string host,int port) tuple){
            return PortInUse(tuple.port);
        }

        public static IObservable<Unit> DetectOffLineHub(this (string host,int port) tuple){
            return Observable.Defer(() => Observable.Start(() => Observable.While(() => tuple.PortInUse(),Observable.Empty<Unit>().Delay(TimeSpan.FromMilliseconds(500)))).Merge())
                    .Concat(Unit.Default.AsObservable()).FirstAsync()
                    .Select(unit => unit)
                    .TraceRXLoggerHub();
        }


//        public static IObservable<(string host, int port)> DetectOnlineHub(this IEnumerable<(string Host, int port)> source){
//            return Observable.Interval(TimeSpan.FromMilliseconds(300))
//                .SelectMany(l => source.Where(_ => !_.PortInUse()));
//        }

        public static IObservable<(string host, int port)> DetectOnlineHub(this (string host,int port) tuple){
            return Observable.Defer(() => Observable.Start(() => Observable.While(() => !tuple.PortInUse(),Observable.Empty<Unit>().Delay(TimeSpan.FromMilliseconds(500)))).Merge())
                    .Select(unit => unit)
                    .Concat(Unit.Default.AsObservable().Select(unit => unit)).FirstAsync()
                    .RepeatWhen(ob =>ob.SelectMany(o => tuple.DetectOffLineHub().Select(unit => unit)))
                    .Select(unit => tuple)
                    .TraceRXLoggerHub()
                    .Finally(() => { })
                
                ;
        }

        public static IEnumerable<(string Host, int port)> ClientPortsList(this XafApplication application){
            return application.ModelLoggerPorts().SelectMany(ports => ports.LoggerPorts).OfType<IModelLoggerClientRange>()
                .TraceRXLoggerHub()
                .SelectMany(range => Enumerable.Range(range.StartPort, range.EndPort-range.StartPort)
                    .Select(i => (range.Host, port: i))).ToEnumerable();
        }

        public static IObservable<(string Host, int port)> ServerPortsList(this XafApplication application){
            return application.ModelLoggerPorts()
                .SelectMany(ports => ports.LoggerPorts.OfType<IModelLoggerServerPort>().Select(_ => (_.Host,_.Port)))
                .TraceRXLoggerHub();
        }

        private static IObservable<IModelServerPorts> ModelLoggerPorts(this XafApplication application){
            return application.ToReactiveModule<IModelReactiveModuleLogger>().Select(logger => logger.ReactiveLogger).Cast<IModelServerPorts>()
                .Select(logger => logger).Cast<IModelServerPorts>();
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

        private static ServerPort ToServerPort(this (string host,int port)tuple){
            return new ServerPort(tuple.host, tuple.port, ServerCredentials.Insecure);
        }


        public static ITraceEventHub NewClient(this ServerPort serverPort,TraceEventReceiver receiver){
            var defaultChannel = new Channel(serverPort.Host, serverPort.Port, ChannelCredentials.Insecure);
            return StreamingHubClient.Connect<ITraceEventHub, ITraceEventHubReceiver>(defaultChannel,receiver);
        }

    }
}
