using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ViewItemValue.BusinessObjects{
    [RuleCombinationOfPropertiesIsUnique(nameof(ViewItemId)+";"+nameof(ObjectView))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types", Justification = "<Pending>")]
    public class ViewItemValueObject:CustomBaseObject{
        public ViewItemValueObject(Session session) : base(session){
        }

        string _objectView;

        [Size(255)]
        public string ObjectView{
            get => _objectView;
            set => SetPropertyValue(nameof(ObjectView), ref _objectView, value);
        }

        string _viewItemId;

        public string ViewItemId{
            get => _viewItemId;
            set => SetPropertyValue(nameof(ViewItemId), ref _viewItemId, value);
        }

        string _viewItemValue;

        [Size(-1)]
        public string ViewItemValue{
            get => _viewItemValue;
            set => SetPropertyValue(nameof(ViewItemValue), ref _viewItemValue, value);
        }
    }
}