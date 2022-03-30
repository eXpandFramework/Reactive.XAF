using System.ComponentModel;
using DevExpress.Xpo;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    public  class LongFullNameTestObject : TestBaseObject,ISequentialNumber {
        private long _sequentialNumber;


        public LongFullNameTestObject(Session session)
            : base(session) {
        }
        [Browsable(false)]
        public long SequentialNumber {
            get => _sequentialNumber;
            set => SetPropertyValue("SequentialNumber", ref _sequentialNumber, value);
        }

        
        
    }
}