using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;

namespace Xpand.XAF.Agnostic.Specifications.Artifacts{
    public abstract class BaseSpecs:IDisposable{
        public void Dispose(){
            XpoTypesInfoHelper.Reset();
            XafTypesInfo.HardReset();
        }
    }
}