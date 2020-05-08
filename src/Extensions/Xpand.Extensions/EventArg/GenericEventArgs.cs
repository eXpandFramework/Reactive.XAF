using System.ComponentModel;

namespace Xpand.Extensions.EventArg{
    public class GenericEventArgs<T> : HandledEventArgs{
        public GenericEventArgs(T instance) => Instance = instance;

        public T Instance { get; }
    }
}