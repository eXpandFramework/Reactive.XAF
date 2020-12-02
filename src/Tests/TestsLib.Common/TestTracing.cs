using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.Persistent.Base;

namespace Xpand.TestsLib.Common{
    public class TestTracing : Tracing{
        readonly Subject<Exception> _exceptions=new Subject<Exception>();

        public IObservable<Exception> Exceptions => _exceptions.AsObservable();

        public override void LogError(Exception exception){
            _exceptions.OnNext(exception);
            base.LogError(exception);
        }
    }
}