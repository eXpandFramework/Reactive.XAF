using DevExpress.ExpressApp;
using Moq;

namespace Xpand.XAF.Agnostic.Specifications.Artifacts{
    class XafApplicationMock:Mock<XafApplication> {
        public XafApplicationMock() {
            CallBase = true;
        }

        public sealed override bool CallBase {
            get => base.CallBase;
            set => base.CallBase = value;
        }
    }
}