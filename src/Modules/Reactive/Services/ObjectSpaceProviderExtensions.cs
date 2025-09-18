using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.FaultHub;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static partial class ObjectSpaceProviderExtensions{
        public static IObservable<TResult> NewObjectSpace<TResult>(this IObjectSpaceProvider provider,Func<IObjectSpace, IObservable<TResult>> factory) 
            => (provider == null ? Observable.Empty<TResult>() : Observable.Using(provider.CreateObjectSpace, factory))
                .PushStackFrame();

        
    }
}