using System;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.AppDomainExtensions;

namespace Xpand.Extensions.Reactive.Utility{
	public static partial class Utility {
        public static IObservable<T> ObserveOnWindows<T>(this IObservable<T> source, SynchronizationContext synchronizationContext) =>
			AppDomain.CurrentDomain.IsHosted() ? source : source.ObserveOn(synchronizationContext);
	}
}