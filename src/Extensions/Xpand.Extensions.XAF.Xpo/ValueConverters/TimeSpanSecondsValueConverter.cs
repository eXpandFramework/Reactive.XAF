using System;
using DevExpress.Xpo.Metadata;
using Xpand.Extensions.Numeric;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class TimeSpanSecondsValueConverter:ValueConverter {
        public override object ConvertToStorageType(object value) 
            => ((TimeSpan?)value)?.TotalSeconds.Round() ?? 0;

        public override object ConvertFromStorageType(object value) => TimeSpan.FromSeconds(((long)(value ?? 0L)));

        public override Type StorageType => typeof(long);

    }
}