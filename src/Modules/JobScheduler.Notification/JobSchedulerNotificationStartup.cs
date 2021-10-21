// using System;
// using DevExpress.ExpressApp.Blazor;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.Extensions.DependencyInjection;
// using Xpand.Extensions.Blazor;
//
// namespace Xpand.XAF.Modules.JobScheduler.Notification {
//     public class BlazorStartupFilter : IStartupFilter {
//         protected static BlazorApplication BlazorApplication;
//
//         public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) 
//             => app => {
//                 next(app);
//                 BlazorApplication = app.ApplicationServices.GetRequiredService<ISharedXafApplicationProvider>().Application;
//                 
//             };
//     }
//
//     public class JobSchedulerNotificationStartup : IHostingStartup {
//         
//         
//
//         public void Configure(IWebHostBuilder builder)
//             => builder.ConfigureServices(services
//                 => {
//                 services.AddSingleton<IStartupFilter, BlazorStartupFilter>();
//                 // services.TryAddSingleton(typeof(IValueManagerStorageAccessor),AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Blazor.Services.ValueManagerStorageAccessor"));
//                 // services.TryAddSingleton<IValueManagerStorageAccessor, ValueManagerStorageAccessor>();
//                 // var buildServiceProvider = services.BuildServiceProvider();
//                 // BlazorApplication = buildServiceProvider.GetRequiredService<ISharedXafApplicationProvider>().Application;
//             });
//         // => services.AddSingleton<IStartupFilter, BlazorStartupFilter>());
//     }
// }