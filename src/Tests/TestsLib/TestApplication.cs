using System.Windows.Forms;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Win;
using Moq;

namespace TestsLib{
    public class TestWinApplication : WinApplication{

        protected override LayoutManager CreateLayoutManagerCore(bool simple){
            var controlMock = new Mock<Control>(){CallBase = true};
            var layoutManagerMock = new Mock<LayoutManager>();
            layoutManagerMock.Setup(_ => _.LayoutControls(It.IsAny<IModelNode>(), It.IsAny<ViewItemsCollection>())).Returns(controlMock.Object);
            return layoutManagerMock.Object;
        }

        protected override string GetModelCacheFileLocationPath(){
            return null;
        }

        protected override string GetDcAssemblyFilePath(){
            return null;
        }

        public override void StartSplash(){
            
        }

        protected override string GetModelAssemblyFilePath(){
            return null;
        }
    }
    public class TestWebApplication : WebApplication{
        protected override bool CanLoadTypesInfo(){
            return true;
        }

        protected override bool IsSharedModel => false;
    }
}