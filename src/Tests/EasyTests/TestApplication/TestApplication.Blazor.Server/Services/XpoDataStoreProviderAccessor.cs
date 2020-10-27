using System;
using DevExpress.ExpressApp.Xpo;

namespace TestApplication.Blazor.Server.Services {
    public class XpoDataStoreProviderAccessor {
        public IXpoDataStoreProvider DataStoreProvider { get; set; }
    }
}
