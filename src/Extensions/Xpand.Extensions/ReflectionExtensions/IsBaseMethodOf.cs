using System.Reflection;

namespace Xpand.Extensions.ReflectionExtensions{
	public partial class ReflectionExtensions{
		public static bool IsBaseMethodOf(this MethodInfo baseMethod, MethodInfo method) => baseMethod.DeclaringType != method.DeclaringType && baseMethod == method.GetBaseDefinition();
	}
}