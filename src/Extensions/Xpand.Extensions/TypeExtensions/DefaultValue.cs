using System;
using Xpand.Extensions.Fasterflect;

namespace Xpand.Extensions.TypeExtensions{
	public static partial class TypeExtensions{
		public static object DefaultValue(this Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;
		
		public static T DefaultValue<T>(this Type t) => t.IsValueType||t.IsArray ? t.CreateInstance<T>() : default;
	}
}