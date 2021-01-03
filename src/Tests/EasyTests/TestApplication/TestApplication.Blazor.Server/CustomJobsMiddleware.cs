using System.Reactive.Concurrency;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Http;

namespace TestApplication.Blazor.Server {
    public class CustomJobsMiddleware {
        private readonly RequestDelegate _next;
        private readonly SharedXafApplicationProvider _sharedXafApplicationProvider;
        private readonly IValueManagerStorageContainerInitializer _valueManagerStorageContainerInitializer;
        public CustomJobsMiddleware(RequestDelegate next, SharedXafApplicationProvider sharedXafApplicationProvider,
            IValueManagerStorageContainerInitializer valueManagerStorageContainerInitializer) {
            _next = next;
            _sharedXafApplicationProvider = sharedXafApplicationProvider;
            _valueManagerStorageContainerInitializer = valueManagerStorageContainerInitializer;
        }
        public async Task Invoke(HttpContext context, IScheduler scheduler) {
            string requestPath = context.Request.Path.Value.TrimStart('/');
            string schemeName = context.Request.Query["JobParams"];

            if(requestPath.StartsWith("api/customJobs")) {
                  _valueManagerStorageContainerInitializer.Initialize();

                // JobParams jobParams = await JsonSerializer.DeserializeAsync<JobParams>(context.Request.Body);
                // await LoadJobs(scheduler, jobParams);
            }
            else {
                await _next(context);
            }
        }

        // private async Task LoadJobs(IScheduler scheduler, JobParams jobParams) {
        //     if(jobParams.ThrowSomeExceptionForTests) {
        //         throw new Exception("Some Test Exception during job start process");
        //     }
        //
        //     TriggerKey triggerKey = new TriggerKey(jobParams.IdentityName, jobParams.IdentityGroup);
        //     var trigger = TriggerBuilder.Create()
        //         .WithIdentity(triggerKey)
        //         .WithCronSchedule(jobParams.CronExpression)
        //         //.ForJob("myJob", "group1")
        //         .Build();
        //
        //     IDictionary<string, object> map = new Dictionary<string, object>()
        //     {                {"Current Date Time", $"{jobParams.DateTime}" },
        //         {"Tickets needed", $"{jobParams.TicketsNeeded}" },
        //         {"Concert Name", $"{jobParams.ConcertName}" },
        //         {"SharedApplication", _sharedXafApplicationProvider.SharedApplication },
        //         {"ValueManagerStorage", _sharedXafApplicationProvider.SharedApplicationValueManagerStorage },
        //         {"ValueManagerStorageContainerInitializer", _valueManagerStorageContainerInitializer }
        //     };
        //     JobKey jobKey = new JobKey(jobParams.TaskID);
        //     IJobDetail job = JobBuilder.Create<ScheduleTask>()
        //         .WithIdentity(jobKey)
        //         .SetJobData(new JobDataMap(map))
        //         .Build();
        //
        //     if(!(await scheduler.CheckExists(triggerKey)) && !(await scheduler.CheckExists(jobKey))) {
        //         await scheduler.ScheduleJob(job, trigger);
        //     }
        // }
    }
}