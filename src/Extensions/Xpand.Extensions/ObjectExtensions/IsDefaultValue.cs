using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.ObjectExtensions{
	public static partial class ObjectExtensions{
		public static bool IsDefaultValue<TSource>(this TSource source){
			var def = default(TSource);
			return def != null ? def.Equals(source) : source == null;
		}

		public static bool IsDefaultValue(this object source) 
            => source == null || source.Equals(source.GetType().DefaultValue());
    }
}