![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.JobScheduler.Hangfire.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.JobScheduler.Hangfire.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/JobScheduler.Hangfire.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AJobScheduler.Hangfire) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/JobScheduler.Hangfire.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AJobScheduler.Hangfire)
# About 

The `JobScheduler.Hangfire` package integrates Hangfire for fire & forget job schedules.

## Details

---

**Credits:** to companies that [sponsor](https://github.com/sponsors/apobekiaris) parts of this package.

---

This `Blazor only` module is valuable when you want to schedule background processes.

### Job Scheduling

Follow the next steps:
1. Configure the Hangfire [default storage](https://docs.hangfire.io/en/latest/configuration/using-sql-server.html). Additionally consult Hangfire docs to configure/implement any other Hangfire related scenario, there are no restrictions.
2. Add a model assembly `job source` pointing to the assembly containing your Job types.<br><br>
    ![image](https://user-images.githubusercontent.com/159464/103508193-4b7dcb80-4e69-11eb-82be-7fa109720368.png)
3. Mark any type with a default constructor with the `Xpand.XAF.Modules.JobScheduler.Hangfire.JobProviderAttribute`. 
   ```c#
    [JobProvider]
    public class Job {
    }
   ```
4. All public methods without parameters can be scheduled using the UI or code.
   ```c#
    [JobProvider]
    public class Import {
        [JobProvider("Customize-Name")]
        public void DailyOrders() {
            throw new NotImplementedException();
        }
    }

    //Schedule the DailyOrders method every day
    var job = objectSpace.CreateObject<Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects.Job>();
    job.JobType = job.JobTypes.First();
    job.JobMethod = job.JobMethods.First();
    job.CronExpression =
        objectSpace.FirstOrDefault<CronExpression>(expression => expression.Name == nameof(Cron.Daily));
    objectSpace.CommitChanges();

    //trigger the new Import().DailyOrder() now
    job.Trigger();

    //pause-resume the DailyOrders
    job.Pause();
    Job.Resume();
   ```
   Note that the methods are scheduled in the background therefore the `HttpContext` is not available.
5. Use DI to inject object instances in the constructor for e.g. to inject an `IServiceProvider` and later use it to get a BlazorApplication use the next snippet. Note that depending on your needs a NonSecuredObjectSpace should be created if your did not explicitly authenticate. 
   ```c#
    [JobProvider]
    public class Import {
        public IServiceProvider Provider { get; }
        public Import() { }
        public Import(IServiceProvider provider) {
            Provider = provider;
        }

        [JobProvider("Customize-Name")]
        public async Task DailyOrders() {
            await ServiceProvider.RunWithStorageAsync(application => Observable.Using(() => application.CreateNonSecuredObjectSpace(true,true), objectSpace 
                => Observable.Range(0, 10).Do(i => {
                        var order = objectSpace.CreateObject<Order>();
                        order.OrderID = i;
                    }).Finally(objectSpace.CommitChanges)));
        }
    }
   ```
   Note that the BlazorApplication is not authenticated and the default constructor must exist.
6. Use a Job descendant to pass job specific parameters.
   ```c#
    public class CustomJob:Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects.Job {
        public CustomJob(Session session) : base(session) { }
        int _ordersCount;

        public int OrdersCount {
            get => _ordersCount;
            set => SetPropertyValue(nameof(OrdersCount), ref _ordersCount, value);
        }
    }

    //same as #5
    [JobProvider] 
    public async Task ImportOrders(PerformContext context) {
        var jobId = context.JobId();
        await ServiceProvider.RunWithStorageAsync(application => Observable.Using(() => application.CreateNonSecuredObjectSpace(true,true),
            objectSpace => { var ordersCount=objectSpace.GetObjectsQuery<CustomJob>()
                        .First(job1 => job1.Id == jobId).OrdersCount
                    for (int i = 0; i < ordersCount; i++) {
                        var order = objectSpace.CreateObject<Order>();
                        order.OrderID = i;
                    }
                    objectSpace.CommitChanges();
                }));
    }

   ```
7. Observe job state.
   ```cs
    Xpand.XAF.Modules.JobScheduler.Hangfire.JobSchedulerService.JobState.Subscribe(state => {
        var job = state.JobWorker.Job;
    });
   ```

In the screencast you can see `how to declare, schedule, pause, resume and get more details` for a Job. Consult the previous code snippets as the video record uses outdated api.

<twitter tags="#Hangfire #Blazor">

[![Xpand XAF Modules JobScheduler Hangfire](https://user-images.githubusercontent.com/159464/103535471-6bc57e80-4e99-11eb-9c59-f54b9abb2023.gif)
](https://youtu.be/0LJ2bM1CfMg)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/0LJ2bM1CfMg)

---

### ChainJobs

I the next screencast:

1. We create a new `ExecuteActionJob` that will schedule the `Email` action for a Product.
2. We create a new `ObjectStateNotificationJob` for new Products.
3. We add a new `ChainedJob` to run te Email ExecuteActionJob after a successful with new Products execution of the ObjectStateNotification job.
4. We check the recipient email to verify that emails send for the new Products found.

<twitter tags="#Hangfire #Blazor #ChainedJob">

[![Xpand XAF Modules JobScheduler Hangfire ChainJobs](https://user-images.githubusercontent.com/159464/140794868-3cf822b7-1542-47b6-b174-245cdc2524dc.gif)
](https://youtu.be/AV0lCSxETxU)
</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/3KRhNgOAFnQ)

All Chained jobs will run in parallel after successful execution of the parent job as in the next snippet. 

```c#
[JobProvider]
public async Task<bool> Execute(PerformContext context)
    => await Task.FromResult(true);
```

### ExecuteActionJob

The `ExecutionActionJob` uses XAF application instances to create the configured View and assign it to a frame so all XAF artifacts gets in place. Then it selects the `ExecuteActionJob.SelectedObjectsCriteria` and executes the related action on them.

In the screencast you can see how we use the `Xpand.XAF.Modules.JobScheduler.HangFire` package to create an `ExecuteActionJob` where we `schedule` the `Delete` action on the `Product_ListView`.

<twitter tags="#Hangfire #Blazor #ExecuteActionJob">

[![Jobsscheduler HangFire XAF Actions](https://user-images.githubusercontent.com/159464/114714259-9cd80980-9d3a-11eb-89f3-e5dccdb4c180.gif)
](https://youtu.be/3KRhNgOAFnQ)
</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/3KRhNgOAFnQ)

> To configure the ExecuteActionJob `Action` lookup list use the Application/ReactiveModules/JobScheduler/Actions model node.

> If you want to run background code with your action you have to configure the Scheduler to keep the Scope alive. For this call `action.CustomizeExecutionFinished()` when you create the action and `action.ExecutionFinished()` after your background job finishes (e.g. EmailModule/EmailService).

### Security

Hangfire Dashboard access can be configured using the XAF Security system as shown:

<twitter tags="#Hangfire #Blazor #Security #Dashboard">

[![Hangfire Security Dashboard](https://user-images.githubusercontent.com/159464/109437432-7893c800-7a2d-11eb-967d-da3c6f90dabf.gif)
](https://youtu.be/Ep_DqomR6D8)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/Ep_DqomR6D8)

**Possible future improvements:**

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.JobScheduler.Hangfire`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.HangfireModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net5.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Validation.Blazor**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
 |**DevExpress.ExpressApp.ConditionalAppearance**|**Any**
 |**DevExpress.ExpressApp.CodeAnalysis**|**Any**
 |**DevExpress.ExpressApp**|**Any**
 |**DevExpress.ExpressApp.Blazor**|**Any**
 |**DevExpress.ExpressApp.Validation**|**Any**
 |**DevExpress.ExpressApp.Validation.Blazor**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
 |**DevExpress.Persistent.Base**|**Any**
|Xpand.Extensions.Blazor|4.212.3
 |Xpand.Extensions.Reactive|4.212.3
 |Xpand.Extensions.XAF|4.212.3
 |Xpand.Extensions|4.212.3
 |Xpand.Extensions.XAF.Xpo|4.212.3
 |[Xpand.XAF.Modules.Blazor](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Blazor)|4.212.3
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.212.3
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |JetBrains.Annotations|2021.3.0
 |System.Reactive|5.0.0
 |System.Interactive|5.0.0
 |Hangfire.AspNetCore|1.7.24
 |Xpand.Patcher|2.0.30
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.212.3

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobScheduler.HangfireModule))
```



### Tests

The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/JobScheduler.Hangfire)

