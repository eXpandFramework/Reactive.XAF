using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using Xpand.XAF.Modules.CloneModelView;

namespace TestApplication.Module.Common {
    [DefaultClassOptions]
    [CloneModelView(CloneViewType.DetailView, TaskBulkUpdateDetailView)]
    public class TestTask:Task {
        public const string TaskBulkUpdateDetailView = "TaskBulkUpdate_DetailView";
        public TestTask(Session session) : base(session) { }
    }
}