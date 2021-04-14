using System;
using DevExpress.Xpo.Metadata;
using Xpand.Extensions.AppDomainExtensions;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {
    public class ObjectTypeValueConverter:ValueConverter {
        public override object ConvertToStorageType(object value) 
            => value is NonPersistentObjects.ObjectType objectType ? objectType.Type?.FullName : null;

        public override object ConvertFromStorageType(object value) {
            var fullName = $"{value}";
            return !string.IsNullOrEmpty(fullName) ? new NonPersistentObjects.ObjectType(AppDomain.CurrentDomain.GetAssemblyType(fullName)) : null;
        }

        public override Type StorageType { get; } = typeof(string);
    }
}