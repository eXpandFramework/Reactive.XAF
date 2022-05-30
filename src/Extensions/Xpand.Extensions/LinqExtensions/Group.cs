using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IGrouping<TK, TV> AsGroup<TK, TV>(this IEnumerable<TV> source, TK key) 
            => Create(key, source);

        private static IGrouping<TK, TV> Create<TK, TV>(TK key, IEnumerable<TV> source) 
            => new SimpleGroupWrapper<TK, TV>(key, source);

        internal class SimpleGroupWrapper<TK, TV> : IGrouping<TK, TV>{
            private readonly IEnumerable<TV> _source;

            public SimpleGroupWrapper(TK key, IEnumerable<TV> source){
                _source = source ?? throw new NullReferenceException("source");
                Key = key;
            }

            public TK Key{ get; }

            public IEnumerator<TV> GetEnumerator() => _source.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _source.GetEnumerator();
        }
    }
}