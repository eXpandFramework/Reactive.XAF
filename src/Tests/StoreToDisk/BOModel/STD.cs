using System.Security.Cryptography;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.StoreToDisk.Tests.BOModel {
    [StoreToDisk(nameof(Name),DataProtectionScope.LocalMachine,nameof(Secret))]
    public class STD:CustomBaseObject {
        public STD(Session session) : base(session) { }

        string _secret;

        public string Secret {
            get => _secret;
            set => SetPropertyValue(nameof(Secret), ref _secret, value);
        }

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
}