using System.Diagnostics.CodeAnalysis;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.BO{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public interface IJSNE {
        long Index { get; set; }
        string Name { get; set; }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class JSNE:XPCustomBaseObject, IJSNE {
        public JSNE(Session session) : base(session){
        }

        long _index;

        [Key(true)]
        public long Index {
            get => _index;
            set => SetPropertyValue(nameof(Index), ref _index, value);
        }
        
        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class JSNE2:XPCustomBaseObject,IJSNE{
        public JSNE2(Session session) : base(session){
        }

        long _index;

        [Key(true)]
        public long Index {
            get => _index;
            set => SetPropertyValue(nameof(Index), ref _index, value);
        }
        
        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
}
