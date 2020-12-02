using System;
using System.Linq.Expressions;

namespace Xpand.Extensions.TypeExtensions{
	public static partial class TypeExtensions{
		public static object DefaultValue(this Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;
	}
}