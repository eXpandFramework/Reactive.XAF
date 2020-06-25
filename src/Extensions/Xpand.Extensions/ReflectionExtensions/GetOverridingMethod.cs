using System;
using System.Linq;
using System.Reflection;

namespace Xpand.Extensions.ReflectionExtensions{
	public partial class ReflectionExtensions{
		public static MethodInfo GetOverridingMethod(this Type type, MethodInfo baseMethod) =>
			type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod)
				.FirstOrDefault(baseMethod.IsBaseMethodOf);
	}
}