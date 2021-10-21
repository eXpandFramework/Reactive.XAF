using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive.Tests.Common {
    public abstract class ReactiveCommonAppTest:ReactiveCommonTest {
        protected XafApplication Application;

        public override void Init() {
            Application = DefaultReactiveModule().Application;
        }

        public override void Dispose() { }

        protected override void ResetXAF(){ }

        public override void Cleanup() {
            base.Cleanup();
            Application?.Dispose();
        }
    }
}