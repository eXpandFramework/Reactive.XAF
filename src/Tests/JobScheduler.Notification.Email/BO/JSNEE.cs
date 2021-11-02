using System.Diagnostics.CodeAnalysis;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests.BO{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class JSNEE:XPCustomBaseObject {
        public JSNEE(Session session) : base(session){
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
