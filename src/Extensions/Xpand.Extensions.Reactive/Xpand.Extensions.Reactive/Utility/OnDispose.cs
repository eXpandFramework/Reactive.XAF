using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> OnDispose<T>(this IObservable<T> source, Action<NotificationKind> disposed)
            => Observable.Create<T>(o => {
                Notification<T> last = null;
                var disposable = Disposable.Create(() => disposed(last == null ? NotificationKind.OnNext : last.Kind));
                return new CompositeDisposable(
                    source.Materialize().Do(x => last = x).Dematerialize().Subscribe(o), disposable);
            });
        
        public static T DisposeWith<T>(this T disposable, CompositeDisposable container) where T : IDisposable {
            if (container == null) {
                throw new ArgumentNullException(nameof(container));
            }
            container.Add(disposable);
            return disposable;
        }
    }
}