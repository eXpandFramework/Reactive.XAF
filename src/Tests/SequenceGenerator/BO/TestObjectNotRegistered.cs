using System.ComponentModel;
using DevExpress.Xpo;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    [PublicAPI]
    public  class TestObjectNotRegistered : TestBaseObject ,ISequentialNumber{
        private long _sequentialNumber;
        
        public TestObjectNotRegistered(Session session)
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