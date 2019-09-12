using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.GridListEditor.Tests.BOModel{
    [DefaultClassOptions]
    public class GLE:BaseObject{
        public GLE(Session session) : base(session){
        }

        int _age;

        public int Age{
            get => _age;
            set => SetPropertyValue(nameof(Age), ref _age, value);
        }

    }
}