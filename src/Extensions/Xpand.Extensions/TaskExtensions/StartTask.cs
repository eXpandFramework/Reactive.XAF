using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.TaskExtensions{
    public static partial class TaskExtensions{
        public static Task<T> StartTask<T>(this Func<T> func,ApartmentState apartmentState = ApartmentState.STA){
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() => {
                try{
                    tcs.SetResult(func());
                }
                catch (Exception e){
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(apartmentState);
            thread.Start();
            return tcs.Task;
        }

        public static Task<T> StartTask<T>(this TaskFactory taskFactory, Func<T> func,ApartmentState apartmentState = ApartmentState.STA){
            return func.StartTask(apartmentState);
        }
    }
}