namespace Xpand.Extensions.ObjectExtensions{
	public static class ObjectExtensions{
		public static bool IsDefaultValue<TSource>(this TSource source){
			var def = default(TSource);
			return def != null ? def.Equals(source) : source == null;
		}
	}
}