using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Subjects {
    public class ReplayLastPerKeySubject<T, TKey> : ISubject<T> {
        private readonly Func<T, TKey> _keySelector;
        private readonly ReplaySubject<ReplaySubject<T>> _subjects;
        private readonly IObservable<T> _mergedSubjects;
        private readonly Dictionary<TKey, ReplaySubject<T>> _perKey;

        public ReplayLastPerKeySubject(Func<T, TKey> keySelector) {
            _keySelector = keySelector;
            _subjects = new ReplaySubject<ReplaySubject<T>>();
            _mergedSubjects = _subjects.Merge();
            _perKey = new Dictionary<TKey, ReplaySubject<T>>();
        }

        public void OnNext(T value) {
            var key = _keySelector(value);
            if (!_perKey.TryGetValue(key, out var subject)) {
                subject = new ReplaySubject<T>(1);
                _perKey.Add(key, subject);
                _subjects.OnNext(subject);
            }

            subject.OnNext(value);
        }

        public void OnCompleted() {
            _subjects.OnCompleted();
            _subjects.Subscribe(subject => subject.OnCompleted());
        }

        public void OnError(Exception error) => _subjects.OnError(error);

        public IDisposable Subscribe(IObserver<T> observer) => _mergedSubjects.Subscribe(observer);
    }
}