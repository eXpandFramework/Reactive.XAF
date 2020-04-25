using System.ComponentModel;
using System.Diagnostics;
using DevExpress.Xpo;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    // ReSharper disable once UseNameofExpression

    [DebuggerDisplay("{ToString()}")]
    [UsedImplicitly]
    public  class TestObject : TestBaseObject, ISequentialNumber{
        private long _sequentialNumber;

        public TestObject(Session session)
            : base(session) {

        }
        [Browsable(false)]
        public long SequentialNumber {
            get => _sequentialNumber;
            set => SetPropertyValue("SequentialNumber", ref _sequentialNumber, value);
        }

        public string Title{ get; set; }
        public override string ToString(){
            return Title??base.ToString();
        }
        
        
    }
}

