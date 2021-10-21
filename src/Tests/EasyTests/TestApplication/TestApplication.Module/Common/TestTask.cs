using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace TestApplication.Module.Common {
    [DefaultClassOptions]
    public class TestTask:Task {
        public TestTask(Session session) : base(session) { }
    }
}