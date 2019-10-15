using System;
using Xpand.TestsLib;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub.Tests{
    class ClientWinApp:TestWinApplication,ILoggerHubClientApplication{
        public ClientWinApp():base(typeof(ReactiveLoggerHubModule)){
            this.ConfigureModel<ReactiveLoggerHubModule>()
                .Subscribe();
        }
    }
}