using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;

namespace TestApplication.Win{
    public class TestWinApplication:WinApplication{
        public TestWinApplication(){
            DatabaseUpdateMode=DatabaseUpdateMode.Never;
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args){
            args.ObjectSpaceProvider=new XPObjectSpaceProvider(new MemoryDataStoreProvider(),true);
        }
    }
}