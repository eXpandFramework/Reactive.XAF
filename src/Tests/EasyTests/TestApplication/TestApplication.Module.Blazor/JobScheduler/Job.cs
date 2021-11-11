using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire;

namespace TestApplication.Module.Blazor.JobScheduler {
    [JobProvider]
    public class Job {
        public IServiceProvider ServiceProvider { get; }
        public Job() { }
        [ActivatorUtilitiesConstructor]
        public Job(IServiceProvider provider) => ServiceProvider = provider;

        [JobProvider]
        public async Task<bool> ImportOrders() 
            => await ServiceProvider.RunWithStorageAsync(application => Observable.Using(
                application.CreateNonSecuredObjectSpace, objectSpace 
                    => Observable.Range(0, 10).Do(i => {
                        var order = objectSpace.CreateObject<Order>();
                        order.OrderID = i;
                    }).Finally(objectSpace.CommitChanges)).To(true));

        public void Failed() {
            throw new NotImplementedException();
        }


    }

}