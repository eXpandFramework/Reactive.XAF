using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Workflow.Tests.BOModel{
    [DefaultClassOptions]
    public class WF(Session session) : CustomBaseObject(session) {
        string _status;

        public string Status {
            get => _status;
            set => SetPropertyValue(nameof(Status), ref _status, value);
        }

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
}