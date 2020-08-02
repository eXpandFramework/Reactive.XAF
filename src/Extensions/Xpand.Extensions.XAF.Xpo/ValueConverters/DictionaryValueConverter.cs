using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class DictionaryValueConverter : ValueConverter{
        private const string KeyDelimeter = "ﮙ";
        private const string Delimeter = "";
        public override Type StorageType => typeof(string);

        public override object ConvertToStorageType(object value){
            var s = ((Dictionary<string, string>)value)?.Aggregate<KeyValuePair<string, string>, string>(null,
                (current, o) => current + o.Key + KeyDelimeter + o.Value + Delimeter);

            return s?.TrimEnd(Delimeter.ToCharArray());
        }

        public override object ConvertFromStorageType(object value){
            if (value == null) return null;
            var split = value.ToString().Split(Delimeter.ToCharArray());
            if (value.ToString().IndexOf(KeyDelimeter, StringComparison.Ordinal) > -1)
                return split.Select(s => s.Split(KeyDelimeter.ToCharArray())).ToDictionary(
                    strings => strings[0].TrimStart('['),
                    strings => strings.Length == 1 ? null : strings[1].Trim().TrimEnd(']'));
            return new Dictionary<string, string>();
        }
    }
}