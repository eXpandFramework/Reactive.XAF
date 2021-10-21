using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Xpo;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib.Blazor{
    public class XpoDataStoreProviderAccessor {
        public IXpoDataStoreProvider DataStoreProvider { get; set; }
    }

    public class TestBlazorApplication : BlazorApplication, ITestApplication,ISharedBlazorApplication{
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        public TestBlazorApplication(Type sutModule, bool transmitMessage = true, bool handleExceptions=true):this() {
            TransmitMessage = transmitMessage;
            SUTModule = sutModule;
            TraceClientConnected = this.ClientConnect();
            TraceClientBroadcast = this.ClientBroadcast();
            this.WhenSetupComplete().FirstAsync()
                .Select(application => application.ObjectSpaceProvider.DataStoreProvider())
                .Do(provider => {
                    var dataStoreProviderAccessor = ServiceProvider.GetService<XpoDataStoreProviderAccessor>();
                    if (dataStoreProviderAccessor != null) dataStoreProviderAccessor.DataStoreProvider = provider;
                })
                .Subscribe();
        }

        public bool TransmitMessage { get; }

        public IObservable<Unit> TraceClientBroadcast{ get; set; }
        public IObservable<Unit> TraceClientConnected{ get; set; }
        
        public Type SUTModule{ get; }

        public TestBlazorApplication() {
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
        }

        protected override void OnCreateCustomObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            var provider = ServiceProvider.GetRequiredService<XpoDataStoreProviderAccessor>().DataStoreProvider;
            if (provider != null) {
                args.ObjectSpaceProvider = this.NewObjectSpaceProvider(provider);
                args.ObjectSpaceProviders.Add(ServiceProvider.GetService<NonPersistentObjectSpaceProvider>()??new NonPersistentObjectSpaceProvider(TypesInfo, null));
            }
            else {
                base.OnCreateCustomObjectSpaceProvider(args);
            }
        }

        protected override string GetModelCacheFileLocationPath() => null;

        protected override string GetDcAssemblyFilePath() => null;

        protected override string GetModelAssemblyFilePath() => $@"{AppDomain.CurrentDomain.ApplicationPath()}\ModelAssembly{Guid.NewGuid()}.dll";
        public bool UseNonSecuredObjectSpaceProvider { get; set; }
    }

}