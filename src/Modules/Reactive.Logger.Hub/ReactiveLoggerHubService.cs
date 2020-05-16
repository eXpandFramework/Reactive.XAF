using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using DevExpress.ExpressApp;
using Grpc.Core;
using MagicOnion.Client;
using MagicOnion.Server;
using MessagePack;
using MessagePack.Resolvers;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Utility;
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
            var startServer = application.StartServer().Publish().RefCount();
            var client = Observable.Start(application.ConnectClient).Merge().Publish().RefCount();

            application.CleanUpHubResources( startServer);

            var saveServerTraceMessages = application.SaveServerTraceMessages().Retry(application).Publish().RefCount();
            return startServer.ToUnit()
                .Merge(client.ToUnit())
                .Merge(saveServerTraceMessages.ToUnit())
                .Merge(application.WhenViewOnFrame(typeof(TraceEvent))
                    .SelectMany(frame => saveServerTraceMessages.LoadTracesToListView(frame)))
                .Retry(application);

        }

        public static void CleanUpHubResources(this XafApplication application, IObservable<Server> startServer){
            application.WhenDisposed().Zip(startServer, (tuple, server) => server.ShutDownServer())
                .Concat()
                .FirstOrDefaultAsync()
                .Subscribe();
        }

        private static IObservable<Unit> ShutDownServer(this Server server){
            return server.ShutdownAsync().ToObservable().TakeUntil(Observable.Timer(TimeSpan.FromSeconds(5)));
        }

        private static IObservable<Unit> LoadTracesToListView(this IObservable<TraceEvent[]> source,Frame frame){
            var synchronizationContext = SynchronizationContext.Current;
            return source.Select(_ => _)
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
            return !(application is ILoggerHubClientApplication)? Observable.Empty<ITraceEventHub>()
                : application.WhenCompatibilityChecked().FirstAsync().SelectMany(_ => application.DetectServer().ConnectClient());
        }

        public static IObservable<IPEndPoint> DetectServer(this XafApplication application){
            return application.ClientPortsList().ToArray().Select(point => point).ToArray().Listening().TraceRXLoggerHub().Select(point => point);
        }

        public static IObservable<ITraceEventHub> ConnectClient(this IObservable<IPEndPoint> source){
            return source.SelectMany(point => {
                var newClient = point.ToServerPort().NewClient(Receiver);
                return newClient.ConnectAsync().ToObservable()
                    .Merge(Unit.Default.ReturnObservable()).To(newClient);
            })
            .TraceRXLoggerHub()
            .Retry();
        }

        public static IEnumerable<IPEndPoint> ClientPortsList(this XafApplication application){
            
            return application.ModelLoggerPorts().SelectMany(ports => ports.LoggerPorts).OfType<IModelLoggerClientRange>()
                .TraceRXLoggerHub()
                .SelectMany(range => Enumerable.Range(range.StartPort, range.EndPort-range.StartPort)
                    .Select(port => {
                        var host = range.Host;
                        return IPEndPoint(host, port);
                    })).Merge()
                .ToEnumerable();
        }

        private static IObservable<IPEndPoint> IPEndPoint(string host, int port){
            var isIP = Regex.IsMatch(host, @"\A\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b\z");
            if (isIP){
                return new IPEndPoint(IPAddress.Parse(host), port).ReturnObservable();
            }

            return Dns.GetHostAddressesAsync(host).ToObservable()
                .Select(addresses => new IPEndPoint(addresses.Last(), port));
        }

        public static IObservable<IPEndPoint> ServerPortsList(this XafApplication application){
            return application.ModelLoggerPorts()
                .SelectMany(ports => ports.LoggerPorts.OfType<IModelLoggerServerPort>().ToObservable().SelectMany(_ => IPEndPoint(_.Host,_.Port)));
        }

        private static IObservable<IModelServerPorts> ModelLoggerPorts(this XafApplication application){
            return application.ToReactiveModule<IModelReactiveModuleLogger>().Select(logger => logger.ReactiveLogger).Cast<IModelServerPorts>()
                .Where(ports => ports.LoggerPorts.Enabled)
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

        private static ServerPort ToServerPort(this IPEndPoint endPoint){
            return new ServerPort(endPoint.Address.ToString(), endPoint.Port, ServerCredentials.Insecure);
        }


        public static ITraceEventHub NewClient(this ServerPort serverPort,TraceEventReceiver receiver){
            var defaultChannel = new Channel(serverPort.Host, serverPort.Port, ChannelCredentials.Insecure);
            return StreamingHubClient.Connect<ITraceEventHub, ITraceEventHubReceiver>(defaultChannel,receiver);
        }

    }
}
