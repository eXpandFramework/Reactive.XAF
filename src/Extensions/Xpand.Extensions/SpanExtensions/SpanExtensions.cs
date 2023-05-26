using System;

namespace Xpand.Extensions.SpanExtensions {
    public static class SpanExtensions {
        public static decimal ToDecimal(this ReadOnlySpan<char> span) {
            decimal.TryParse(span, out var result);
            return result;
        }

        public static int ToInt32(this ReadOnlySpan<char> span) {
            int.TryParse(span, out var result);
            return result;
        }

        public static long ToInt64(this ReadOnlySpan<char> span) {
            long.TryParse(span, out var result);
            return result;
        }

        public static float ToSingle(this ReadOnlySpan<char> span) {
            float.TryParse(span, out var result);
            return result;
        }

        public static double ToDouble(this ReadOnlySpan<char> span) {
            double.TryParse(span, out var result);
            return result;
        }


        public static ushort ToUInt16(this ReadOnlySpan<char> span) {
            ushort.TryParse(span, out var result);
            return result;
        }

        public static uint ToUInt32(this ReadOnlySpan<char> span) {
            uint.TryParse(span, out var result);
            return result;
        }

        public static ulong ToUInt64(this ReadOnlySpan<char> span) {
            ulong.TryParse(span, out var result);
            return result;
        }
        
        public static bool ToBoolean(this ReadOnlySpan<char> span) {
            bool.TryParse(span, out var result);
            return result;
        }
        
        public static DateTime? ToNullableDateTime(this ReadOnlySpan<char> span) 
            => span.IsEmpty ? null : DateTime.TryParse(span, out var result) ? result : null;
        public static DateTime ToDateTime(this ReadOnlySpan<char> span) {
            DateTime.TryParse(span, out var result);
            return result;
        }

        public static DateTimeOffset ToDateTimeOffset(this ReadOnlySpan<char> span) {
            DateTimeOffset.TryParse(span, out var result);
            return result;
        }
        
        public static Guid ToGuid(this ReadOnlySpan<char> span) {
            Guid.TryParse(span, out var result);
            return result;
        }
        
        public static TimeSpan ToTimeSpan(this ReadOnlySpan<char> span) {
            TimeSpan.TryParse(span, out var result);
            return result;
        }
        
        
        
    }
}