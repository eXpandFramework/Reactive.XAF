using System;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.ObjectExtensions{
	public static partial class ObjectExtensions{
        public static bool IsDefaultValue<TSource>(this TSource source) {
	        var def = default(TSource);
	        return def != null ? def.Equals(source) : typeof(TSource) == typeof(object)
			        ? source == null || source.Equals(source.GetType().DefaultValue()) : source == null;
        }

		public static bool IsDefaultValue(this object source) 
            => source == null || source.Equals(source.GetType().DefaultValue());
		
		public static bool IsDefaultValue(this object source,Type objectType) 
            => objectType.DefaultValue()==source;
    }
}