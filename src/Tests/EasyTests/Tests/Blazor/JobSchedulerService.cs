using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Hangfire;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.TestsLib.Common.BO;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects;

namespace Web.Tests {
    public static class JobSchedulerService {
        public static async Task TestJobScheduler(this ICommandAdapter adapter) => await adapter.TestJob();

        public static async Task TestChainJob(this ICommandAdapter adapter, string appicationPath) {
            adapter.NewEmailJob();
            adapter.NewChainedJob();
            adapter.NewChainedProduct();
            adapter.Execute(new NavigateCommand("JobScheduler.Job",false));
            adapter.Execute(new SelectObjectsCommand<Job>(job => job.Id,new[] { "ChainJob" }));
            await adapter.TestEmailCreation(appicationPath,nameof(NewChainedProduct), () => adapter.Execute(
                    new ActionCommand(nameof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobSchedulerService.Trigger))));
            adapter.Execute(new NavigateCommand("Default.Product"));
            adapter.Execute(new SelectObjectsCommand<Product>(product => product.ProductName,
                new[] { nameof(NewChainedProduct) }));
            adapter.Execute(new ActionDeleteCommand());
        }

        private static void NewChainedProduct(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand("Default.Product"));
            adapter.Execute(new ActionCommand(Actions.New));
            adapter.Execute(new FillObjectViewCommand<Product>((product => product.ProductName, nameof(NewChainedProduct)),
                (product => product.Id, "2")));
            adapter.Execute(new ActionCommand(Actions.Save));
        }

        private static void NewChainedJob(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand("JobScheduler.Job"));
            adapter.Execute(new ActionCommand("New", nameof(ObjectStateNotification)));
            adapter.Execute(new FillObjectViewCommand<ObjectStateNotification>((job => job.Object, nameof(Product)),
                (job => job.Id, "ChainJob")));
            adapter.Execute(new ActionCommand($"{nameof(Job.ChainJobs).CompoundName()}.New"));
            adapter.Execute(new FillObjectViewCommand<ChainJob>((job => job.Job, "EmailJob")));
            adapter.Execute(new ActionCommand(Actions.OK));
            adapter.Execute(new ActionCommand(Actions.Save));
        }

        private static void NewEmailJob(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand("JobScheduler.Job"));
            adapter.Execute(new ActionCommand("New", nameof(ExecuteActionJob)));
            adapter.Execute(new FillObjectViewCommand<ExecuteActionJob>(
                (job => job.Action, nameof(Xpand.XAF.Modules.Email.EmailService.Email)),
                (job => job.Object, nameof(Product)), (job => job.View, $"{nameof(Product)}_DetailView"),
                (job => job.Id, "EmailJob"),(job => job.SelectedObjectsCriteria,$"{nameof(Product.ProductName)}='{nameof(NewChainedProduct)}'")));
            adapter.Execute(new ActionCommand(Actions.Save));
        }

        public static async Task TestExecuteActionJob(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand("Default.Product"));
            adapter.Execute(new SelectObjectsCommand<Product>(product => product.ProductName,new []{"ProductName0","ProductName1"}));
            adapter.Execute(new ActionDeleteCommand());
            adapter.Execute(new ActionCommand(Actions.New));
            adapter.Execute(new FillObjectViewCommand<Product>((product => product.ProductName, "deleteme")));
            adapter.Execute(new ActionCommand(Actions.Save));

            adapter.Execute(new NavigateCommand("JobScheduler.Job"));
            adapter.Execute(new ActionCommand("New",nameof(ExecuteActionJob)));
            adapter.Execute(new FillObjectViewCommand<ExecuteActionJob>((job => job.Action, "Delete"),
                (job => job.Object, nameof(Product)), (job => job.View, $"{nameof(Product)}_ListView"),
                (job => job.Id,"executeDelete")));
            adapter.Execute(new FillObjectViewCommand<ExecuteActionJob>((job => job.SelectedObjectsCriteria,
	            $"{nameof(Product.ProductName)}='deleteme'")));
            adapter.Execute(new ActionCommand(Actions.Save));
            adapter.Execute(new ActionCommand("Trigger"));
            adapter.Execute(new NavigateCommand("Default.Product"));
            
            await adapter.Execute(() => {
                adapter.Execute(new ActionCommand(Actions.Refresh));
                var checkListViewCommand = new CheckListViewCommand(nameof(Product.ProductName));
                checkListViewCommand.AddRows(new []{"deleteme"});
                checkListViewCommand.ExpectException = true;
                adapter.Execute(checkListViewCommand);
            });
        }

        private static async Task TestJob(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand("JobScheduler.Job",false));
            adapter.CreateJob().TestPauseResume();
            await adapter.TestJob(WorkerState.Succeeded, 1);

            adapter.Execute(new NavigateCommand("JobScheduler.Job",false));
            adapter.Execute(new ProcessRecordCommand<Job, Job>((job => job.Id, nameof(JobSchedulerService))));
            adapter.Execute(new FillObjectViewCommand((nameof(Job.JobMethod), "Failed")));

            adapter.Execute(new ActionCommand(Actions.Save));

            await adapter.TestJob(WorkerState.Failed, 2);
        }

        private static ICommandAdapter CreateJob(this ICommandAdapter adapter) {
            adapter.Execute(new ActionCommand(Actions.New));
            adapter.Execute(new FillObjectViewCommand<Job>((job => job.Id, nameof(JobSchedulerService)),(job => job.CronExpression, nameof(Cron.Yearly))));
            adapter.Execute(new FillObjectViewCommand((nameof(Job.JobType),"Job"),(nameof(Job.JobMethod),"ImportOrders".CompoundName())));
            adapter.Execute(new ActionCommand(Actions.Save));
            return adapter;
        }

        private static async Task TestJob(this ICommandAdapter adapter, WorkerState workerState, int workersCount) {
            adapter.Execute(new ActionCommand("Trigger"));

            adapter.Execute(new ActionCommand(nameof(Job.Workers)));
            await adapter.Execute(() => adapter.Execute(new ActionCommand(Actions.Refresh),new CheckListViewCommand<Job>(job => job.Workers, workersCount)));
            adapter.Execute(new SelectObjectsCommand<Job>(job => job.Workers,nameof(JobWorker.State),new []{workerState.ToString()}));
            //
            // adapter.Execute(new ActionCommand("Dashboard"));
            // await adapter.Execute(() => {
            //     var webDriver = adapter.Driver();
            //     webDriver.SwitchTo().Window(webDriver.WindowHandles[1]);
            //     webDriver.Url.ShouldContain("/hangfire/jobs/details/");
            //     webDriver.Close();
            //     webDriver.SwitchTo().Window(webDriver.WindowHandles[0]);
            // });
            adapter.Execute(new ProcessRecordCommand<Job,JobWorker>(job => job.Workers,(worker => worker.State,workerState.ToString())));
            adapter.Execute(new ActionCommand(Actions.OK));
        }

        private static void TestPauseResume(this ICommandAdapter adapter) {
            adapter.Execute(new ActionAvailableCommand("Resume") {ExpectException = true});
            adapter.Execute(new ActionCommand("Pause"));
            adapter.Execute(new ActionAvailableCommand("Pause") {ExpectException = true});
            adapter.Execute(new ActionCommand("Resume"));
            adapter.Execute(new ActionAvailableCommand("Pause"));
            adapter.Execute(new ActionAvailableCommand("Resume") {ExpectException = true});
            
        }
    }
}