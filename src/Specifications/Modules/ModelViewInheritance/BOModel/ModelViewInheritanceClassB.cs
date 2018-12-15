using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance.BOModel{
    public class ModelViewInheritanceClassB: BaseObject{
        public ModelViewInheritanceClassB(Session session) : base(session){
        }

        string _test1;

        public string Test1 {
            get => _test1;
            set => SetPropertyValue(nameof(Test1), ref _test1, value);
        }

        string _test2;

        public string Test2 {
            get => _test2;
            set => SetPropertyValue(nameof(Test2), ref _test2, value);
        }
    }
}