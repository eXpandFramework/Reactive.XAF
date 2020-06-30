namespace Xpand.Extensions.XAF.AppDomainExtensions{
    public interface IXAFAppDomain{
        System.AppDomain AppDomain{ get; }    
    }

    class XAFAppDomain:IXAFAppDomain{
        public XAFAppDomain(System.AppDomain appDomain) => AppDomain = appDomain;

        public System.AppDomain AppDomain{ get;  }
    }
    public static partial class AppDomainExtensions{
        public static IXAFAppDomain XAF(this System.AppDomain appDomain) => new XAFAppDomain(appDomain);

    }


}