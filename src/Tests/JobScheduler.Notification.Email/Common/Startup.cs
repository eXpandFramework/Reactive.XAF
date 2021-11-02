﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Blazor;

[assembly: HostingStartup(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.HangfireStartup))]
 [assembly: HostingStartup(typeof(HostingStartup))]
 [assembly:HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]
namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests.Common {
    public class Startup : XafHostingStartup<EmailNotificationModule> {
        public Startup(IConfiguration configuration) : base(configuration) { }

    }

}