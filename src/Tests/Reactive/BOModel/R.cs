using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Reactive.Tests.BOModel{
    public class R2(Session session) : CustomBaseObject(session), IR;

    public interface IR {
        
    }
    [DefaultClassOptions][OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    public class R:CustomBaseObject,IR{
        public R(Session session) : base(session){
        }

        string _test;

        public string Test {
            get => _test;
            set => SetPropertyValue(nameof(Test), ref _test, value);
        }

        string _test1;

        public string Test1 {
            get => _test1;
            set => SetPropertyValue(nameof(Test1), ref _test1, value);
        }

        string _test3;

        public string Test3 {
            get => _test3;
            set => SetPropertyValue(nameof(Test3), ref _test3, value);
        }

        [Association("R-RChilds")][Aggregated]
        public XPCollection<RChild> RChilds => GetCollection<RChild>();
    }

    public class RChild(Session session) : CustomBaseObject(session) {
        R _r;

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        

        [Association("R-RChilds")]
        public R R {
            get => _r;
            set => SetPropertyValue(nameof(R), ref _r, value);
        }
    }
}