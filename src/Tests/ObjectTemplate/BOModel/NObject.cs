using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Notification.Tests.BOModel {
    public class NObject:CustomBaseObject {
        public NObject(Session session) : base(session) { }

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
}