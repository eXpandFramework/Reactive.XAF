using System.ComponentModel;
using DevExpress.Xpo;
using Xpand.XAF.Modules.SequenceGenerator.Tests.BO;

// ReSharper disable once CheckNamespace
namespace Tests.SequenceGenerator.BO.Tests.SequenceGenerator.BO.Tests.SequenceGenerator.BO.Tests.SequenceGenerator.BO.Tests.SequenceGenerator.BO.Tests.SequenceGenerator.BO.BO.Tests.SequenceGenerator.BO.Tests.SequenceGenerator.BO.Tests.SequenceGenerator.BO{
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