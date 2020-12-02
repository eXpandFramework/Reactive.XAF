using System;
using DevExpress.Xpo.Metadata;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {
    public class ObjectStringValueConverter:ValueConverter {
        public override object ConvertToStorageType(object value) 
            => value is ObjectString objectString ? objectString.Name : null;

        public override object ConvertFromStorageType(object value) 
            => new ObjectString((string) value);

        public override Type StorageType { get; } = typeof(string);
    }
}