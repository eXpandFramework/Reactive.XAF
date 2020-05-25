using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive.Services{
    public interface IXAFAppWebAPI{
        XafApplication Application{ get; }
    }
    public interface IXAFAppWinAPI{
        XafApplication Application{ get; }
    }
    class XAFAppWebAPI:IXAFAppWebAPI{
        public XafApplication Application{ get; }

        public XAFAppWebAPI(XafApplication application){
            Application = application;
        }

    }

    class XAFAppWinAPI:IXAFAppWinAPI{
        public XafApplication Application{ get; }

        public XAFAppWinAPI(XafApplication application){
            Application = application;
        }

    }

}