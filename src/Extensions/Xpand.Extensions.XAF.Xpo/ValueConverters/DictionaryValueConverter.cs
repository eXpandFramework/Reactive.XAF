using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class DictionaryValueConverter : ValueConverter{
        private const string KeyDelimiter = "ﮙ";
        private const string Delimiter = "";
        public override Type StorageType => typeof(string);

        public override object ConvertToStorageType(object value){
            var s = ((Dictionary<string, string>)value)?.Aggregate<KeyValuePair<string, string>, string>(null,
                (current, o) => current + o.Key + KeyDelimiter + o.Value + Delimiter);

            return s?.TrimEnd(Delimiter.ToCharArray());
        }

        public override object ConvertFromStorageType(object value){
            if (value == null) return null;
            var split = value.ToString().Split(Delimiter.ToCharArray());
            return value.ToString().IndexOf(KeyDelimiter, StringComparison.Ordinal) > -1
                ? split.Select(s => s.Split(KeyDelimiter.ToCharArray())).ToDictionary(
                    strings => strings[0].TrimStart('['),
                    strings => strings.Length == 1 ? null : strings[1].Trim().TrimEnd(']'))
                : new Dictionary<string, string>();
        }
    }
}