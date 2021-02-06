using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace TestApplication.Module.PositionInListView {
    [DefaultClassOptions]
    public class PositionInListViewObject:BaseObject {
        public PositionInListViewObject(Session session) : base(session) { }
        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        [PersistentAlias(nameof(Id))]
        public int IdVisible => (int) EvaluateAlias(nameof(IdVisible));

        int _id;

        public int Id {
            get => _id;
            set {
                if (SetPropertyValue(nameof(Id), ref _id, value)) {
                    OnChanged(nameof(IdVisible));
                }
            }
        }
    }
}
