using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Castle.DynamicProxy;
using Fasterflect;
using Xpand.Extensions.ReflectionExtensions;

namespace Xpand.TestsLib.Common{
	public static class InvocationExtensions{
		private static readonly ProxyGenerator Generator;

		[Serializable]
		private class Interceptor : IInterceptor{
			Subject<IInvocation> _invocationSubject=new Subject<IInvocation>();

			public IObservable<IInvocation> Invocation => _invocationSubject.AsObservable();

			public void Intercept(IInvocation invocation){
				if(_invocationSubject.HasObservers){
					_invocationSubject.OnNext(invocation);
				}
				else{
					invocation.Proceed();
				}
			}
		}

		private class ProxyHook : IProxyGenerationHook{
			private readonly HashSet<MethodInfo> _methods;

			public ProxyHook(HashSet<MethodInfo> methods) => _methods = methods;

			public void MethodsInspected(){
			}

			public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo){
			}

			public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo) => _methods.Contains(methodInfo);
		}

		static InvocationExtensions() => Generator = new ProxyGenerator();

		public static IObservable<IInvocation> WhenInvocation<T>(this T theObject) where T : IProxyTargetAccessor =>
			theObject.GetInterceptors().Cast<Interceptor>().ToObservable()
				.SelectMany(interceptor => interceptor.Invocation);

		public static IProxyTargetAccessor ToProxy<T,TResult>(this T theObject,params Expression<Func<T, TResult>>[] memberSelectors) where T : class{
			var methods = memberSelectors.Select(expression => expression.Body).Cast<MethodCallExpression>()
				.SelectMany(expression => typeof(T).Methods(expression.Method.Name).Where(info => info==expression.Method||expression.Method.IsBaseMethodOf(info)));
			var interceptors = new Interceptor();
			var proxyGenerationOptions = new ProxyGenerationOptions(new ProxyHook(new HashSet<MethodInfo>(methods)));
			return (IProxyTargetAccessor) Generator.CreateClassProxyWithTarget(theObject, proxyGenerationOptions, interceptors);
		}
	}

	public class A{
		public static T Is<T>() => default;
	}
}