using BrokeroTests.SequenceGenerator.BO;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    [PublicAPI]
    public class Parent:BaseObject{
        public Parent(Session session) : base(session){
        }

        [Association("Parent-Childs")][Aggregated]
        public XPCollection<Child> Childs => GetCollection<Child>(nameof(Childs));

    }
    
    [PublicAPI]
    public class ParentSequencial:Parent,ISequentialNumber{
        public ParentSequencial(Session session) : base(session){
        }

        long _sequentialNumber;

        public long SequentialNumber{
            get => _sequentialNumber;
            set => SetPropertyValue(nameof(SequentialNumber), ref _sequentialNumber, value);
        }
    }
    
    [PublicAPI]
    public class Child:TestObject{
        public Child(Session session) : base(session){
        }


        Parent _parent;

        [Association("Parent-Childs")]
        public Parent Parent{
            get => _parent;
            set => SetPropertyValue(nameof(Parent), ref _parent, value);
        }
    }
}