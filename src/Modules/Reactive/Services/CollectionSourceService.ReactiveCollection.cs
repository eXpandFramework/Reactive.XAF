using System.Collections;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
    public class ReactiveCollection<T>(IObjectSpace objectSpace)
        : DynamicCollection(objectSpace, typeof(T), null, null, false), IList<T> {
        public IEnumerator<T> GetEnumerator() => ((IEnumerable) this).GetEnumerator().Cast<T>();

        public void Add(T item) => base.Add(item);

        public bool Contains(T item) => base.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => base.CopyTo(array, arrayIndex);

        public bool Remove(T item) {
            if (Objects.IndexOf(item)>-1) {
                base.Remove(item);
                return true;
            }

            return false;
        }

        public int IndexOf(T item) => base.IndexOf(item);

        public void Insert(int index, T item) => base.Insert(index, item);

        T IList<T>.this[int index] {
            get => (T) base[index];
            set => base[index]=value;
        }
    }

}