using System;
using System.ComponentModel;

namespace Xpand.Extensions.EventArgExtensions{
    public class GenericEventArgs<T> : HandledEventArgs{
        public GenericEventArgs(T instance) => Instance = instance;

        public GenericEventArgs(){
        }

        public T Instance { get; private set; }

        public bool SetInstance(Func<T,T> apply) {
            Instance = apply(Instance);
            return true;
        }

        // public void SetInstance(Action<T> apply) => apply(Instance);
    }
}