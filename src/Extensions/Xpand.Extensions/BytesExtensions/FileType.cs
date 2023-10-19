using System.Linq;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        private static bool IsMaskMatch(this byte[] byteArray, int offset, params byte[] mask) 
            => byteArray != null && byteArray.Length >= offset + mask.Length &&
               !mask.Where((t, i) => byteArray[offset + i] != t).Any();

        public static string FileType(this byte[] value)
            => value switch {
                { Length: > 0 } when value.IsMaskMatch(0, 77, 77) || value.IsMaskMatch(0, 73, 73) => "tiff",
                { Length: > 0 } when value.IsMaskMatch(1, 80, 78, 71) => "png",
                { Length: > 0 } when value.IsMaskMatch(0, 71, 73, 70, 56) => "gif",
                { Length: > 0 } when value.IsMaskMatch(0, 255, 216) => "jpeg",
                { Length: > 0 } when value.IsMaskMatch(0, 66, 77) => "bmp",
                _ => ""
            };
    }
}