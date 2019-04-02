using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;

namespace Xpand.XAF.Agnostic.Tests.Artifacts{
    public abstract class BaseTest:IDisposable{
        public virtual void Dispose(){
            XpoTypesInfoHelper.Reset();
            XafTypesInfo.HardReset();
        }
    }
}