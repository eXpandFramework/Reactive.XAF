using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Agnostic.Tests.Modules.ModelViewInheritance.BOModel{
    public class TagMvi:CustomBaseObject{
        public TagMvi(Session session) : base(session){
        }

        string _name;
        ABaseMvi _aBaseMvi;

        [Association("ABaseMvi-TagMvis")]
        public ABaseMvi ABaseMvi{
            get => _aBaseMvi;
            set => SetPropertyValue(nameof(ABaseMvi), ref _aBaseMvi, value);
        }
        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
}