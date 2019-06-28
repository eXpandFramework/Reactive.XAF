using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.CloneMemberValue.Tests.BOModel{
    public class ACmv:CustomBaseObject{
        public ACmv(Session session) : base(session){
        }

        string _primitiveProperty;

        public string PrimitiveProperty{
            get => _primitiveProperty;
            set => SetPropertyValue(nameof(PrimitiveProperty), ref _primitiveProperty, value);
        }

        BCmv _bCmv;

        public BCmv BCmv{
            get => _bCmv;
            set => SetPropertyValue(nameof(BCmv), ref _bCmv, value);
        }
    }
}