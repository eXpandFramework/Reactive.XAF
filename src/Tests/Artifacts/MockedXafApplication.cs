using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Moq;

namespace Xpand.XAF.Agnostic.Tests.Artifacts{
    public abstract class MockedXafApplication:XafApplication{
        protected override ListEditor CreateListEditorCore(IModelListView modelListView, CollectionSourceBase collectionSource){
            var mock = new Mock<ListEditor>{CallBase = true};
            mock.Setup(editor => editor.SupportsDataAccessMode(CollectionSourceDataAccessMode.Client)).Returns(true);
            mock.Setup(editor => editor.GetSelectedObjects()).Returns(new object[0]);
//            mock.SetupProperty(editor => editor.FocusedObject).Raise(editor => editor.CallMethod("OnSelectionChanged"));
            return mock.Object;
        }
    }
}