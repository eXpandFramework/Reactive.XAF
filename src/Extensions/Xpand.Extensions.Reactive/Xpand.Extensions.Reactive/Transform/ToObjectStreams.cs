using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;

namespace Xpand.Extensions.Reactive.Transform {
    
    public static partial class Transform {
        private static readonly ConcurrentDictionary<Type, Func<object, IObservable<object>>> ConvertersCache = new();
        private static readonly MethodInfo SelectMethod;
        static Transform(){
            SelectMethod = typeof(Observable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Observable.Select) && m.GetParameters().Length == 2);
        }

        public static IEnumerable<IObservable<object>> ToObjectStreams(this IEnumerable<object> source) {
            foreach (var obs in source) {
                if (obs == null) {
                    throw new ArgumentNullException(nameof(source), "Source collection cannot contain null elements.");
                }

                var type = obs.GetType();
                var iobservableInterface = type.GetInterfaces().FirstOrDefault(i
                    => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IObservable<>));
                if (iobservableInterface == null) {
                    throw new ArgumentException($"Item does not implement IObservable<T>: {type.FullName}");
                }

                var genericArg = iobservableInterface.GetGenericArguments()[0];
                if (genericArg == typeof(object)) {
                    yield return (IObservable<object>)obs;
                    continue;
                }

                var converter = ConvertersCache.GetOrAdd(genericArg, argType => {
                    var sourceObservableParameter = Expression.Parameter(typeof(object), "sourceObs");
                    var castSourceObservable = Expression.Convert(sourceObservableParameter,
                        typeof(IObservable<>).MakeGenericType(argType));
                    var selectLambdaParameter = Expression.Parameter(argType, "t");
                    var castToObject = Expression.Convert(selectLambdaParameter, typeof(object));
                    var selectLambda = Expression.Lambda(castToObject, selectLambdaParameter);
                    var genericSelectMethod = SelectMethod.MakeGenericMethod(argType, typeof(object));
                    var selectCall = Expression.Call(null, genericSelectMethod, castSourceObservable, selectLambda);
                    var finalLambda =
                        Expression.Lambda<Func<object, IObservable<object>>>(selectCall, sourceObservableParameter);
                    return finalLambda.Compile();
                });

                yield return converter(obs);
            }
        }
    }
}