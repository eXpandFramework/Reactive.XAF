using System;
using System.Reflection;

namespace Xpand.Extensions.ReflectionExtensions{
	public partial class ReflectionExtensions{
		public static bool HasOverridingMethod(this Type type, MethodInfo baseMethod) => type.GetOverridingMethod(baseMethod) != null;
	}
}