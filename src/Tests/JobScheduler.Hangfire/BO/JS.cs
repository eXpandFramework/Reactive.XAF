using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO{
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [DefaultClassOptions]
    public class JS:CustomBaseObject{
        public JS(Session session) : base(session){
        }

        int _id;

        public int Id {
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }
        
        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
}
