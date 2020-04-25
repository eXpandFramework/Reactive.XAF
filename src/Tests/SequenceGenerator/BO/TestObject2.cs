using System.ComponentModel;
using DevExpress.Xpo;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    public  class TestObject2 : TestBaseObject,ISequentialNumber {
        private long _sequentialNumber;
        
        public TestObject2(Session session)
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