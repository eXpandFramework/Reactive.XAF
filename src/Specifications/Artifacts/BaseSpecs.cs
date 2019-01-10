using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;

namespace DevExpress.XAF.Agnostic.Specifications.Artifacts{
    public abstract class BaseSpecs:IDisposable{
        public void Dispose(){
            XpoTypesInfoHelper.Reset();
            XafTypesInfo.HardReset();
        }
    }
}