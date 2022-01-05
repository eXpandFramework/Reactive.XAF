using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Subjects {
    /// <summary>
    /// Subject with same semantics as <see cref="ValveSubject{T}"/>, but adding flushing out capability 
    /// which allows clearing the valve of any remaining elements before closing.
    /// </summary>
    /// <typeparam name="T">Elements type</typeparam>
    public class FlushableValveSubject<T> : IFlushableValveSubject<T> {
        private readonly BehaviorSubject<ValveSubject<T>> _valvesSubject = new(new ValveSubject<T>());
        private ValveSubject<T> CurrentValve => _valvesSubject.Value;

        public bool IsOpen => CurrentValve.IsOpen;

        public bool IsClosed => CurrentValve.IsClosed;

        public void OnNext(T value) => CurrentValve.OnNext(value);

        public void OnError(Exception error) => CurrentValve.OnError(error);

        public void OnCompleted() {
            CurrentValve.OnCompleted();
            _valvesSubject.OnCompleted();
        }

        public IDisposable Subscribe(IObserver<T> observer) => _valvesSubject.Switch().Subscribe(observer);

        public void Open() => CurrentValve.Open();

        public void Close() => CurrentValve.Close();

        /// <summary>
        /// Discards remaining elements in the valve and reset the valve into a closed state
        /// </summary>
        /// <returns>Replayable observable with any remaining elements</returns>
        public IObservable<T> FlushAndClose() {
            var previousValve = CurrentValve;
            _valvesSubject.OnNext(CreateClosedValve());
            var remainingElements = new ReplaySubject<T>();
            previousValve.Subscribe(remainingElements);
            previousValve.Open();
            return remainingElements;
        }

        private static ValveSubject<T> CreateClosedValve() {
            var valve = new ValveSubject<T>();
            valve.Close();
            return valve;
        }
    }
    public interface IFlushableValveSubject<T> : IValveSubject<T> {
        IObservable<T> FlushAndClose();
    }

    public static class FlushableValveExtensions {
        public static IFlushableValveSubject<T> Synchronize<T>(this IFlushableValveSubject<T> valve) 
            => Synchronize(valve, new object());

        public static IFlushableValveSubject<T> Synchronize<T>(this IFlushableValveSubject<T> valve, object gate) 
            => new SynchronizedFlushableValveAdapter<T>(valve, gate);
    }

    internal class SynchronizedFlushableValveAdapter<T> : SynchronizedValveAdapter<T>, IFlushableValveSubject<T> {
        private readonly object _gate;
        private readonly IFlushableValveSubject<T> _valve;

        public SynchronizedFlushableValveAdapter(IFlushableValveSubject<T> valve, object gate)
            : base(valve, gate) {
            _valve = valve;
            _gate = gate;
        }

        public IObservable<T> FlushAndClose() {
            lock (_gate) {
                return _valve.FlushAndClose();
            }
        }
    }
}