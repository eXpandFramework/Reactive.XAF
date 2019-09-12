using System;
using TestsLib;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub.Tests{
    class ClientWinApp:TestWinApplication,ILoggerHubClientApplication{
        internal ClientWinApp(){
            this.ConfigureModel()
                .Subscribe();
        }
    }
}