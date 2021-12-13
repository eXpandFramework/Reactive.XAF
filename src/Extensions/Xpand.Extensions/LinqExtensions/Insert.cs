using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        private class KeyValuesPair {
            public string Name { get; set; }
            public string[] Values { get; set; }
        }

        public static void Insert(this NameValueCollection col, int index, string name, string value) {
            if (index < 0 || index > col.Count)
                throw new ArgumentOutOfRangeException();

            if (col.GetKey(index) == value) {
                col.Add(name, value);
            }
            else {
                List<KeyValuesPair> items = new List<KeyValuesPair>();
                int size = col.Count;
                for (int i = index; i < size; i++) {
                    string key = col.GetKey(index);
                    items.Add(new KeyValuesPair {
                        Name = key,
                        Values = col.GetValues(index),
                    });
                    col.Remove(key);
                }

                col.Add(name, value);

                foreach (var item in items) {
                    foreach (var v in item.Values) {
                        col.Add(item.Name, v);
                    }
                }
            }
        }
    }
}

