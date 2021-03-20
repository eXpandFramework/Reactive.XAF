using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.NonPersistentObjects {
    [DomainComponent]
    [XafDefaultProperty(nameof(Key))]
    public class KeyValuePairObject:NonPersistentBaseObject {
        public KeyValuePairObject(KeyValuePair<string, string> pair) {
            Key = pair.Key;
            Value = pair.Value;
        }

        private string _key;
        private string _value;

        public string Key {
            get => _key;
            set {
                if (value == _key) return;
                _key = value;
                OnPropertyChanged();
            }
        }

        public string Value {
            get => _value;
            set {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public KeyValuePairObject(string key) {
            Key = key;
        }

    }

    public static class KeyValuePairObjectExtensions {
        public static IEnumerable<KeyValuePairObject> ToKeyValuePairObjects(this  Dictionary<string,string> source) 
            => source.Select(pair => new KeyValuePairObject(pair));
    }


}