using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace Tests.Artifacts{
    public static class AppDomainExtensions{
        public static Task<Unit> Work(this AppDomain ad, Func<Task<Unit>> action){
            return ad.Marshal(action);
        }

        public static Task<Unit> Work<T>(this AppDomain ad, Func<T, Task<Unit>> action, T arg1){
            return ad.Marshal(action, arg1);
        }

        public static Task<T> Marshal<T, T1>(this AppDomain appDomain, Func<T1, Task<T>> function, T1 arg1){
            var m = new MarshalableCompletionSource<T>();
            var t = typeof(RemoteWorker<T>);
            var w = (RemoteWorker<T>) appDomain.CreateInstanceAndUnwrap(t.Assembly.FullName, t.FullName ?? throw new InvalidOperationException());
            w.Run(function, arg1, m);
            return m.Task;
        }

        public static Task<T> Marshal<T>(this AppDomain appDomain, Func< Task<T>> function){
            var m = new MarshalableCompletionSource<T>();
            var t = typeof(RemoteWorker<T>);
            var w = (RemoteWorker<T>) appDomain.CreateInstanceAndUnwrap(t.Assembly.FullName, t.FullName ?? throw new InvalidOperationException());
            w.Run(function, m);
            return m.Task;
        }

        private class RemoteWorker<T> : MarshalByRefObject{
            public void Run(Func<Task<T>> function, MarshalableCompletionSource<T> marshaler){
                function().ContinueWith(t => {
                    if (t.IsFaulted) marshaler.SetException(t.Exception?.InnerExceptions.ToArray());
                    else if (t.IsCanceled) marshaler.SetCanceled();
                    else marshaler.SetResult(t.Result);
                });
            }

            public void Run<T1>(Func<T1, Task<T>> function, T1 arg1, MarshalableCompletionSource<T> marshaler){
                function(arg1).ContinueWith(t => {
                    if (t.IsFaulted) marshaler.SetException(t.Exception?.InnerExceptions.ToArray());
                    else if (t.IsCanceled) marshaler.SetCanceled();
                    else marshaler.SetResult(t.Result);
                });
            }
        }

        private class MarshalableCompletionSource<T> : MarshalByRefObject{
            private readonly TaskCompletionSource<T> _mTcs = new TaskCompletionSource<T>();

            public Task<T> Task => _mTcs.Task;

            public void SetResult(T result){
                _mTcs.SetResult(result);
            }

            public void SetException(Exception[] exception){
                _mTcs.SetException(exception);
            }

            public void SetCanceled(){
                _mTcs.SetCanceled();
            }
        }
    }
}