using System;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
    public class JobProviderAttribute:Attribute {
        public string DisplayName { get; set; }

        public JobProviderAttribute() {
        }

        public JobProviderAttribute(string displayName) => DisplayName = displayName;
    }
}