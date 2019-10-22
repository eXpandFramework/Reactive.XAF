using DevExpress.ExpressApp;

namespace TestApplication.Win{
    public class WinModule:ModuleBase{
        public WinModule(){
            AdditionalExportedTypes.Add(typeof(Customer));
        }
    }
}