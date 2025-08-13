using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        private static readonly MethodInfo SelectMethod;
        static Combine(){
            SelectMethod = typeof(Observable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Observable.Select) && m.GetParameters().Length == 2);

        }

        public static IObservable<Unit> ExecuteTransaction(this IEnumerable<IObservable<object>> source, string transactionName = "Transaction") 
            => source.ToNowObservable().SelectManySequential(obs => obs.DefaultIfEmpty(new object())).BufferUntilCompleted()
                .Select(results => results.OfType<Exception>().ToList())
                .SelectMany(allFailures => !allFailures.Any() ? Unit.Default.Observe() : Observable.Throw<Unit>(new InvalidOperationException($"{transactionName} failed", new AggregateException(allFailures))));

        public static IObservable<Unit> ExecuteTransaction(this IEnumerable<object> source,Func<IObservable<object>, IObservable<object>> resiliencePolicy, string transactionName = "Transaction") 
            => source.AsObservablesOfObject().ExecuteTransaction(resiliencePolicy, transactionName);

        public static IObservable<Unit> ExecuteTransaction<TSource>(this IEnumerable<IObservable<TSource>> source,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy, string transactionName = "Transaction")
            => source.Select((obs, i) => resiliencePolicy(obs).ChainFaultContext(context: [$"{transactionName} - Op:{i + 1}"])
                    .Select(t => (object)t).Catch((FaultHubException ex) => Observable.Return<object>(ex)))
                .ExecuteTransaction(transactionName);

        public static IObservable<Unit> ExecuteTransaction(this IEnumerable<IObservable<Unit>> source, string transactionName = "Transaction") 
            => source.Select(s => s.ToTransactional()).ExecuteTransaction(transactionName);
        
        

        public static IObservable<object> ToTransactional<TSource>(this IObservable<TSource> source) 
            => source.ChainFaultContext().ToObject()
                .Catch((FaultHubException ex) => Observable.Return<object>(ex));
        
        public static IEnumerable<IObservable<object>> AsObservablesOfObject(this IEnumerable<object> source) {
            foreach (var obs in source) {
                if (obs == null) {
                    throw new ArgumentNullException(nameof(source), "Source collection cannot contain null elements.");
                }
                var type = obs.GetType();
                var iobservableInterface = type.GetInterfaces().FirstOrDefault(
                    i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IObservable<>));
                if (iobservableInterface == null) {
                    throw new ArgumentException($"Item does not implement IObservable<T>: {type.FullName}");
                }
                var genericArg = iobservableInterface.GetGenericArguments()[0];
                if (genericArg == typeof(object)) {
                    yield return (IObservable<object>)obs;
                    continue;
                }
                var param = Expression.Parameter(genericArg, "t");
                var castToObject = Expression.Convert(param, typeof(object));
                var lambda = Expression.Lambda(castToObject, param);
                var genericSelect = SelectMethod.MakeGenericMethod(genericArg, typeof(object));

                yield return (IObservable<object>)genericSelect.Invoke(null, [obs, lambda.Compile()]);
            }
            
        }

    }
}