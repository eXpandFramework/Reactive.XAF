using System;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub.Tests{
    class ClientWinApp:TestWinApplication,ILoggerHubClientApplication{
        public ClientWinApp():base(typeof(ReactiveLoggerHubModule)){
            this.ConfigureModel<ReactiveLoggerHubModule>()
                .Subscribe();
        }
    }
}