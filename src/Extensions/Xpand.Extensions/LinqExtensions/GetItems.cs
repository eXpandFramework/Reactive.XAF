using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<T> GetItems<T>(this IEnumerable collection,Func<T, IEnumerable> selector,Func<T,object> distinctSelector=null) {
            HashSet<object> hashSet=null;
            if (distinctSelector!=null){
                hashSet=new HashSet<object>();
            }
            var stack = new Stack<IEnumerable<T>>();
            stack.Push(collection.OfType<T>());

            while (stack.Count > 0) {
                var items = stack.Pop();
                foreach (var item in items){
                    var o = distinctSelector?.Invoke(item);
                    if (hashSet != null ){
                        if (!hashSet.Contains(o)){
                            hashSet.Add(o);
                            yield return item;
                            var children = selector(item).OfType<T>();
                            stack.Push(children);
                        }
                    }
                    else{
                        yield return item;
                        var children = selector(item).OfType<T>();
                        stack.Push(children);
                    }
                    
                }
            }
        }
    }
}
