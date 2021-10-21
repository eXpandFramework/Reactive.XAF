using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace TestApplication.Module.Blazor.JobScheduler.Notification {
    [DefaultClassOptions]
    public class JSN:XPCustomBaseObject {
        public JSN(Session session) : base(session) { }

        int _index;

        [Key(true)]
        public int Index {
            get => _index;
            set => SetPropertyValue(nameof(Index), ref _index, value);
        }
    }
}