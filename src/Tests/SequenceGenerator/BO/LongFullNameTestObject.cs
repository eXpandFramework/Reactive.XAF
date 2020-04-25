using System.ComponentModel;
using DevExpress.Xpo;
using Xpand.XAF.Modules.SequenceGenerator.Tests.BO;

// ReSharper disable once CheckNamespace
namespace BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO{
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