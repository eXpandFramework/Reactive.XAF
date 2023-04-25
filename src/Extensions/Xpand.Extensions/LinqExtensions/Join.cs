using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions {
		public static string JoinString(this IEnumerable source) 
			=> source is string s ? s.JoinString(null) : source.Join(null);

		public static string JoinString(this object source, params object[] objects) 
			=> objects.Prepend(source).Join(null);
		public static string JoinString(this object source, params string[] objects) 
			=> objects.Prepend(source).Join(null);

		public static string Join(this IEnumerable source, string separator) 
			=> source is IEnumerable<string> strings ? strings.Join(separator)
				: source.Cast<object>().Select(o => o.EnsureString()).Join(separator);
		
		public static string Join(this IEnumerable source) 
			=> source.Join(null);

		public static string JoinNewLine(this IEnumerable source) => source?.Join(Environment.NewLine);
		public static string JoinSpace(this IEnumerable source) => source?.Join(" ");
		public static string JoinComma(this IEnumerable source) => source?.Join(",");
		public static string JoinDot(this IEnumerable source) => source?.Join(".");
		public static string JoinCommaSpace(this IEnumerable source) => source?.Join(", ");
		public static string JoinDotSpace(this IEnumerable source) => source?.Join(". ");
		
		public static string Join(this IEnumerable<string> values, string separator) 
			=> ZString.Join(separator, values);
	
		
	}

	
}