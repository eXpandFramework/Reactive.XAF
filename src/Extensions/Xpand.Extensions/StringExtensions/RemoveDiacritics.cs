using System;
using System.Globalization;
using System.Text;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static String RemoveDiacritics(this String s) {
            String normalizedString = s.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (char c in normalizedString) {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }
    }
}