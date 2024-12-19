using System;
using System.Net;
using System.Threading;

namespace Xpand.Extensions.AppDomainExtensions {
    
    public static partial class AppDomainExtensions {
        public static void ConfigureNetwork(this AppDomain appDomain,SecurityProtocolType? securityProtocolType=null, int? connectionLimit = null, int? workerThreads = null, int? asyncThreads = null) {
            if (securityProtocolType.HasValue) {
                // ServicePointManager.SecurityProtocol=securityProtocolType.Value;
                throw new NotImplementedException();
            }

            if (connectionLimit.HasValue) {
                // ServicePointManager.DefaultConnectionLimit = connectionLimit.Value;
                throw new NotImplementedException();
            }

            if (workerThreads.HasValue && asyncThreads.HasValue) {
                ThreadPool.SetMinThreads(workerThreads.Value, asyncThreads.Value);    
            }
            
        }
    }
}