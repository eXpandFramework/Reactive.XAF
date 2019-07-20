using System.Windows.Forms;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Layout;
using Moq;

namespace TestsLib{
    public class TestWinApplication : WinApplication{

        protected override LayoutManager CreateLayoutManagerCore(bool simple){
            if (!simple){
                var controlMock = new Mock<Control>(){CallBase = true};
                var layoutManagerMock = new Mock<WinLayoutManager>(){CallBase = true};
                layoutManagerMock.Setup(_ => _.LayoutControls(It.IsAny<IModelNode>(), It.IsAny<ViewItemsCollection>())).Returns(controlMock.Object);
            
                return layoutManagerMock.Object;
            }

            return new WinSimpleLayoutManager();
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