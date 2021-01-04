using System;
using DevExpress.ExpressApp.Blazor;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire;
using Task = System.Threading.Tasks.Task;

namespace TestApplication.Module.Blazor.JobScheduler {
    [JobProvider]
    public class Job {
        public BlazorApplication Application { get; }
        public Job() { }
        public Job(BlazorApplication application) {
            Application = application;
        }

        public async Task ImportOrders() {
            using var objectSpace = Application.CreateObjectSpace();
            for (int i = 0; i < 10; i++) {
                var order = objectSpace.CreateObject<Order>();
                order.OrderID = i;
            }

            await objectSpace.CommitChangesAsync();
        }

        public void Failed() {
            throw new NotImplementedException();
        }


    }

}