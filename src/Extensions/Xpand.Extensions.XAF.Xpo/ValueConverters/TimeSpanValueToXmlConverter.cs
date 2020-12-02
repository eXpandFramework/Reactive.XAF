using System;
using System.Xml;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class TimeSpanValueToXmlConverter : ValueConverter {
        public override object ConvertFromStorageType(object value) => value == null ? TimeSpan.Zero : XmlConvert.ToTimeSpan((String)value);

        public override object ConvertToStorageType(object value) => value == null ? null : XmlConvert.ToString((TimeSpan)value);
        public override Type StorageType => typeof(String);
    }
}