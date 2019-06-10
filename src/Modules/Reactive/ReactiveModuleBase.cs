using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive{
    public abstract class ReactiveModuleBase:ModuleBase{
        public void Unload(){
            Application.Modules.Remove(this);
            Dispose();
        }
    }
}