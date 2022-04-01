using System.ComponentModel;
using DevExpress.Xpo;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    class CustomSequenceTypeName{
        
    }
    [SequenceStorageKeyName(typeof(CustomSequenceTypeName))]
    public  class CustomSquenceNameTestObject : TestBaseObject,ISequentialNumber {
        private long _sequentialNumber;


        public CustomSquenceNameTestObject(Session session)
            : base(session) {
        }
        [Browsable(false)]
        public long SequentialNumber {
            get => _sequentialNumber;
            set => SetPropertyValue("SequentialNumber", ref _sequentialNumber, value);
        }

        
        
    }
}