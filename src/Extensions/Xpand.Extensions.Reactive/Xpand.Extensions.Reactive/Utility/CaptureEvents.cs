using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Utility {
    
    public static partial class Utility {
        public static Task<CaptureResult<T>> Capture<T>(this IObservable<T> source) 
            => new CaptureObserver<T>().Run(source);

        private sealed class CaptureObserver<T> : IObserver<T> {
            private readonly TaskCompletionSource<CaptureResult<T>> _completionSource = new();
            private readonly List<T> _items = new();
            private readonly Lock _gate = new();
            private Exception _error;
            private bool _isCompleted;
            private IDisposable _upstream = Disposable.Empty;
            private int _terminated;

            internal Task<CaptureResult<T>> Run(IObservable<T> source) {
                _upstream =source.Subscribe(this);
                return _completionSource.Task;
            }


            public void OnNext(T value) {
                if (Volatile.Read(ref _terminated) == 1) return;
                lock (_gate) {
                    if (_terminated != 0) return;
                    _items.Add(value);
                }
            }

            public void OnError(Exception error) {
                if (Interlocked.CompareExchange(ref _terminated, 1, 0) != 0) return;
                _error = error;
                _completionSource.SetResult(new CaptureResult<T>(_items.AsReadOnly(), _error, _isCompleted));
                _upstream.Dispose();
            }

            public void OnCompleted() {
                if (Interlocked.CompareExchange(ref _terminated, 1, 0) != 0) return;
                _isCompleted = true;
                _completionSource.SetResult(new CaptureResult<T>(_items.AsReadOnly(), _error, _isCompleted));
                _upstream.Dispose();
            }
        }
    }
    public sealed record CaptureResult<T>(IReadOnlyList<T> Items, Exception Error, bool IsCompleted) {
        public int ItemCount => Items.Count;
    }

}