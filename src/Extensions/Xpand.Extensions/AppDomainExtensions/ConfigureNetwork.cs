using System;
using System.Net;
using System.Threading;

namespace Xpand.Extensions.AppDomainExtensions {
    
    public static partial class AppDomainExtensions {
        public static void ConfigureNetwork(this AppDomain appDomain, int connectionLimit = 100, int workerThreads = 100, int asyncThreads = 4) {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            ServicePointManager.DefaultConnectionLimit = connectionLimit;
            ThreadPool.SetMinThreads(workerThreads, asyncThreads);
        }
    }
}