using Microsoft.Extensions.Configuration;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.StoreToDisk.Tests.Common {
    public class Startup : XafHostingStartup<StoreToDiskModule> {
        public Startup(IConfiguration configuration) : base(configuration) { }

        
    }
}