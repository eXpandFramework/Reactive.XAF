using Moq;

namespace Xpand.XAF.Agnostic.Tests.Artifacts{
    class XafApplicationMock:Mock<MockedXafApplication> {
        public XafApplicationMock() {
            CallBase = true;
        }

        public sealed override bool CallBase {
            get => base.CallBase;
            set => base.CallBase = value;
        }
    }
}