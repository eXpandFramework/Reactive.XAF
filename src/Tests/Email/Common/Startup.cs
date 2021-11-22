using System;
using Microsoft.Extensions.Configuration;
using Xpand.TestsLib.Blazor;
using Xpand.XAF.Modules.Email.Tests.BOModel;

namespace Xpand.XAF.Modules.Email.Tests.Common {
    public class Startup : XafHostingStartup<EmailModule> {
        public Startup(IConfiguration configuration) : base(configuration) { }

        protected override Type UserType() => typeof(EmailUser);
    }
}