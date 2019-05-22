using DevExpress.ExpressApp;

namespace Xpand.Source.Extensions.XAF{
    public abstract class XafModule : ModuleBase{
        public void Unload(){
            Application.Modules.Remove(this);
            Dispose();
        }
    }
}