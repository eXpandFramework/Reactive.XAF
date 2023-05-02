using System.Globalization;
using System.Linq;
using System.Text;
using Xpand.Extensions.LinqExtensions;


namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string RemoveDiacritics(this string s) 
            => s.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .JoinString();
    }
}