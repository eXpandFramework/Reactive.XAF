using System;

namespace Xpand.Extensions.Reactive.ErrorHandling{
    sealed class ResilientObservable<T> : IObservable<T>
    {
        readonly IObservable<T> _inner;
        internal ResilientObservable(IObservable<T> inner) => _inner = inner;
        public IDisposable Subscribe(IObserver<T> o) => _inner.Subscribe(o);
    }
}