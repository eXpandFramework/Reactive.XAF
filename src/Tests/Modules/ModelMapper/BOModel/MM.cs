using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Tests.Modules.ModelMapper.BOModel{
    public class MM : CustomBaseObject{
        public MM(Session session) : base(session){
        }

        string _test;

        public string Test{
            get => _test;
            set => SetPropertyValue(nameof(Test), ref _test, value);
        }
    }
}