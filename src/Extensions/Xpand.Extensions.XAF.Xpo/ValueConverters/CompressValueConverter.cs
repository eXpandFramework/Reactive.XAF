using System;
using DevExpress.Xpo.Metadata;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class CompressValueConverter:ValueConverter {
        public override object ConvertToStorageType(object value) => value is string s ? s.GZip() : null;

        public override object ConvertFromStorageType(object value) => value is byte[] bytes ? bytes.Unzip() : null;

        public override Type StorageType => typeof(byte[]);
    }
}