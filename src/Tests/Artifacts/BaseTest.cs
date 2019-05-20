using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using IDisposable = System.IDisposable;

namespace Tests.Artifacts{
    
    public abstract class BaseTest : IDisposable{
        public virtual void Dispose(){
            XpoTypesInfoHelper.Reset();
            XafTypesInfo.HardReset();
        }
    }
}