using System;
using System.Globalization;
using System.Text;
using Cysharp.Text;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string RemoveDiacritics(this String s) {
            var normalizedString = s.Normalize(NormalizationForm.FormD);
            using var stringBuilder = ZString.CreateUtf8StringBuilder();
            foreach (var c in normalizedString) {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString();
        }
    }
}