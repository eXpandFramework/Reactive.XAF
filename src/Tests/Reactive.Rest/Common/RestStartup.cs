using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Blazor;

[assembly: HostingStartup(typeof(HostingStartup))]
[assembly: HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.Common {
    public class RestStartup : XafHostingStartup<RestModule> {
        public RestStartup(IConfiguration configuration) : base(configuration) { }

    }

}