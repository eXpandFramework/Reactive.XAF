using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace Xpand.XAF.Agnostic.Specifications.Modules.ModelViewInheritance.BOModel{
    public class ABaseMvi: BaseObject{
        public ABaseMvi(Session session) : base(session){
        }

        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        string _description;

        public string Description{
            get => _description;
            set => SetPropertyValue(nameof(Description), ref _description, value);
        }

        [Association("ABaseMvi-TagMvis")]
        public XPCollection<TagMvi> Tags => GetCollection<TagMvi>(nameof(Tags));
    }
}