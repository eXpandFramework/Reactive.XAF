using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.StoreToDisk.BusinessObjects {
    public class StoreToDiskStorage(Session session) :CustomBaseObject(session) {
        string _typeName;

        [ValueConverter(typeof(CompressValueConverter))]
        public string TypeName {
            get => _typeName;
            set => SetPropertyValue(nameof(TypeName), ref _typeName, value);
        }
    }
}