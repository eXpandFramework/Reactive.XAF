using System;
using Microsoft.Extensions.DependencyInjection;
using Xpand.XAF.Modules.JobScheduler.Hangfire;

namespace TestApplication.Module.Blazor.JobScheduler {
    [JobProvider]
    public class Job {
        public IServiceProvider ServiceProvider { get; }
        public Job() { }
        [ActivatorUtilitiesConstructor]
        public Job(IServiceProvider provider) => ServiceProvider = provider;

        // [JobProvider]
        // public async Task<bool> ImportOrders() 
        //     => await ServiceProvider.RunWithStorageAsync(application => Observable.Using(
        //         () => application.CreateNonSecuredObjectSpace(typeof(Order)),objectSpace 
        //             => Observable.Range(0, 10).Do(i => {
        //                 var order = objectSpace.CreateObject<Order>();
        //                 order.OrderID = i;
        //             }).Finally(objectSpace.CommitChanges)).To(true));

        public void Failed() {
            throw new NotImplementedException();
        }


    }

}