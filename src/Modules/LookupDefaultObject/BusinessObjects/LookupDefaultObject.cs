using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.LookupDefaultObject.BusinessObjects{
    [RuleCombinationOfPropertiesIsUnique(nameof(MemberName)+";"+nameof(ObjectView))]
    public class LookupDefaultObject:CustomBaseObject{
        public LookupDefaultObject(Session session) : base(session){
        }

        string _objectView;

        [Size(255)]
        public string ObjectView{
            get => _objectView;
            set => SetPropertyValue(nameof(ObjectView), ref _objectView, value);
        }

        string _memberName;

        public string MemberName{
            get => _memberName;
            set => SetPropertyValue(nameof(MemberName), ref _memberName, value);
        }

        string _keyValue;

        [Size(-1)]
        public string KeyValue{
            get => _keyValue;
            set => SetPropertyValue(nameof(KeyValue), ref _keyValue, value);
        }
    }
}