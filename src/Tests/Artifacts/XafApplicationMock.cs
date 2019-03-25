using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
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

    public abstract class MockedXafApplication:XafApplication{
        protected override ListEditor CreateListEditorCore(IModelListView modelListView, CollectionSourceBase collectionSource){
            var mock = new Mock<ListEditor>();
            mock.Setup(editor => editor.SupportsDataAccessMode(CollectionSourceDataAccessMode.Client)).Returns(true);
            return mock.Object;
        }
    }
}