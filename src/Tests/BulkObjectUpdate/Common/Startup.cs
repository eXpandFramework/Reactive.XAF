using Microsoft.Extensions.Configuration;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.BulkObjectUpdate.Tests.Common {
    public class Startup : XafHostingStartup<BulkObjectUpdateModule> {
        public Startup(IConfiguration configuration) : base(configuration) { }

    }
    

}