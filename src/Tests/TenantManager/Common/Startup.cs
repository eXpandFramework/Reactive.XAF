using Microsoft.Extensions.Configuration;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.TenantManager.Tests.Common {
    public class Startup : XafHostingStartup<TenantManagerModule> {
        public Startup(IConfiguration configuration) : base(configuration) { }

        
    }
}