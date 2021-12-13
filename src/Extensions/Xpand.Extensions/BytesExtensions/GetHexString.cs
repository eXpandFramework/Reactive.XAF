using System.Linq;
using System.Text;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
         
        public static string GetHexString(this byte[] buff) 
            => buff.Aggregate(new StringBuilder(),
                (sb, b) => sb.AppendFormat("{0:x2}", b), sb => sb.ToString());
    }
}