using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {
    public class SqlDateTimeOverFlowValueConverter : ValueConverter {
        public static readonly DateTime MinValue = new(1753, 1, 1);
        public override Type StorageType => typeof(DateTime);

        public override object ConvertToStorageType(object value) {
            if (value is not DateTime dateTimeValue) return value;
            
            var maxValue = new DateTime(9999, 12, 31);
            return dateTimeValue >= MinValue ? dateTimeValue > maxValue ? maxValue : value
                : MinValue.AddTicks(dateTimeValue.TimeOfDay.Ticks);

        }

        public override object ConvertFromStorageType(object value) 
            => value != null && (DateTime)value == MinValue ? DateTime.MinValue : value;
    }
    public class SqlDateTimeOffSetOverFlowValueConverter : ValueConverter {
        public override Type StorageType => typeof(DateTime);

        public override object ConvertToStorageType(object value) {
            if (value != null) {
                var dateTime = new DateTimeOffset(SqlDateTimeOverFlowValueConverter.MinValue);
                if (dateTime > (DateTimeOffset)value) {
                    var time = ((DateTimeOffset)value).TimeOfDay;
                    DateTimeOffset storageType = dateTime.AddTicks(time.Ticks);
                    return storageType.DateTime;
                }
                dateTime = new DateTimeOffset(new DateTime(9999, 12, 31));
                return dateTime < (DateTimeOffset)value ? dateTime.DateTime : ((DateTimeOffset)value).DateTime;
            }
            return null;
        }

        public override object ConvertFromStorageType(object value) => value != null ? new DateTimeOffset((DateTime)value) : null;
    }
}