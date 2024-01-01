using System;

namespace Xpand.Extensions.EnvironmentExtensions {
    public static class EnvironmentExtensions {
        public static string Get(this EnvironmentVariableTarget target,string name) 
            => Environment.GetEnvironmentVariable(name, target);
    }
}