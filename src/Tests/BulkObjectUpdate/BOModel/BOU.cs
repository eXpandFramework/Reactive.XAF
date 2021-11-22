using System.ComponentModel;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.BulkObjectUpdate.Tests.BOModel{
    [DefaultProperty(nameof(Name))]
    public class BOU:CustomBaseObject{
        public BOU(Session session) : base(session){
        }

        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
    [DefaultProperty(nameof(Name))]
    public class BOU2:CustomBaseObject{
        public BOU2(Session session) : base(session){
        }

        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
    
}