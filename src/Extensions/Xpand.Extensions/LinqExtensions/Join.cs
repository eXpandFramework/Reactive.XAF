using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static string Join(this IEnumerable source, string separator) 
			=> source is IEnumerable<string> strings ? strings.JoinWithBuilder(separator)
				: string.Join(separator, source.Cast<object>());
		
		public static string JoinConcat(this IEnumerable source) 
			=> source.Join("");

		public static string JoinNewLine(this IEnumerable source) => source?.Join(Environment.NewLine);
		public static string JoinSpace(this IEnumerable source) => source?.Join(" ");
		public static string JoinComma(this IEnumerable source) => source?.Join(",");
		public static string JoinCommaSpace(this IEnumerable source) => source?.Join(", ");
		
		public static string JoinStringNewLine(this string source,string second) 
			=> source?.JoinString(Environment.NewLine,second);
		public static string JoinString(this string source,string separator,params string[] values) 
			=> string.Join(separator,values.Prepend(source));
		
		public static string JoinWithBuilder(this IEnumerable<string> values, string separator) 
			=> separator.IsNullOrEmpty() ? ZString.Concat(values) : ZString.Join(separator, values);
	}
}