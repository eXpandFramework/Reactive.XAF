namespace Xpand.XAF.Modules.Reactive.Win {
    public sealed class ReactiveModuleWin : ReactiveModuleBase {
        public ReactiveModuleWin() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }
    }
}
