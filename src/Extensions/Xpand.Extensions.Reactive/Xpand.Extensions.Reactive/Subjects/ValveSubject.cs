using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Subjects {
    /// <summary>
    /// Subject offering Open() and Close() methods, with built-in buffering.
    /// Note that closing the valve in the observer is supported.
    /// </summary>
    /// <remarks>As is the case with other Rx subjects, this class is not thread-safe, in that
    /// order of elements in the output is in-deterministic in the case of concurrent operation 
    /// of Open()/Close()/OnNext()/OnError(). To guarantee strict order of delivery even in the 
    /// case of concurrent access, <see cref="ValveSubjectExtensions.Synchronize{T}(IValveSubject{T})"/> can be used.</remarks>
    /// <typeparam name="T">Elements type</typeparam>
    public class ValveSubject<T> : IValveSubject<T> {
        private enum Valve {
            Open,
            Closed
        }

        private readonly Subject<T> _input = new();
        private readonly BehaviorSubject<Valve> _valveSubject = new(Valve.Open);
        private readonly Subject<T> _output = new();

        public ValveSubject() {
            var valveOperations = _valveSubject.DistinctUntilChanged();
            _input.Buffer(valveOperations.Where(v => v == Valve.Closed),
                    _ => valveOperations.Where(v => v == Valve.Open))
                .SelectMany(t => t).Subscribe(_input);
            _input.Where(_ => _valveSubject.Value == Valve.Open).Subscribe(_output);
        }

        public bool IsOpen => _valveSubject.Value == Valve.Open;

        public bool IsClosed => _valveSubject.Value == Valve.Closed;

        public void OnNext(T value) => _input.OnNext(value);

        public void OnError(Exception error) => _input.OnError(error);

        public void OnCompleted() {
            _output.OnCompleted();
            _input.OnCompleted();
            _valveSubject.OnCompleted();
        }

        public IDisposable Subscribe(IObserver<T> observer) => _output.Subscribe(observer);

        public void Open() => _valveSubject.OnNext(Valve.Open);

        public void Close() => _valveSubject.OnNext(Valve.Closed);
    }

    public interface IValveSubject<T> : ISubject<T> {
        void Open();

        void Close();
    }

    public static class ValveSubjectExtensions {
        public static IValveSubject<T> Synchronize<T>(this IValveSubject<T> valve) => Synchronize(valve, new object());

        public static IValveSubject<T> Synchronize<T>(this IValveSubject<T> valve, object gate) => new SynchronizedValveAdapter<T>(valve, gate);
    }

    internal class SynchronizedValveAdapter<T> : IValveSubject<T> {
        private readonly object _gate;
        private readonly IValveSubject<T> _valve;

        public SynchronizedValveAdapter(IValveSubject<T> valve, object gate) {
            _valve = valve;
            _gate = gate;
        }

        public void OnNext(T value) {
            lock (_gate) {
                _valve.OnNext(value);
            }
        }

        public void OnError(Exception error) {
            lock (_gate) {
                _valve.OnError(error);
            }
        }

        public void OnCompleted() {
            lock (_gate) {
                _valve.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer) => _valve.Subscribe(observer);

        public void Open() {
            lock (_gate) {
                _valve.Open();
            }
        }

        public void Close() {
            lock (_gate) {
                _valve.Close();
            }
        }
    }
}