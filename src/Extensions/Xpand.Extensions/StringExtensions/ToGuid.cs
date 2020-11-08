using System;
using System.Security.Cryptography;
using System.Text;

namespace Xpand.Extensions.StringExtensions{
	public static partial class StringExtensions{
		public static Guid ToGuid(this string s){
			if (!Guid.TryParse(s, out var guid)) {
                using var md5 = MD5.Create();
                var data = md5.ComputeHash(Encoding.Default.GetBytes(s));
                return new Guid(data);
            }

			return guid;
		}
	}
}