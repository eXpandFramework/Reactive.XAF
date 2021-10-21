using NUnit.Framework;
using Xpand.XAF.Modules.ObjectTemplate.Tests.Common;

namespace Xpand.XAF.Modules.ObjectTemplate.Tests {
    public class TemplateTests:CommonAppTest {
        public override void Init() {
            base.Init();
            NewRule();
        }

        [Test]
        public void MethodName() {
            // Application.CompileNotificationTemplate();
        }

    }
}