using System.Security.Cryptography;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.StoreToDisk.Tests.BOModel {
    [StoreToDisk(nameof(Name),DataProtectionScope.LocalMachine,nameof(Secret),nameof(Number),nameof(Dep))]
    public class STD:CustomBaseObject {
        public STD(Session session) : base(session) { }

        STDep _dep;

        public STDep Dep {
            get => _dep;
            set => SetPropertyValue(nameof(Dep), ref _dep, value);
        }
        
        string _secret;

        public string Secret {
            get => _secret;
            set => SetPropertyValue(nameof(Secret), ref _secret, value);
        }

        int _number;

        public int Number {
            get => _number;
            set => SetPropertyValue(nameof(Number), ref _number, value);
        }

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }

    public class STDep : CustomBaseObject {
        public STDep(Session session) : base(session) { }

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
}