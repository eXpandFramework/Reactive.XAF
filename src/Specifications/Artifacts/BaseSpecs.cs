using DevExpress.ExpressApp;

namespace DevExpress.XAF.Agnostic.Specifications.Artifacts{
    public abstract class BaseSpecs{
        protected BaseSpecs(){
            XafTypesInfo.HardReset();
        }
    }
}