namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        
        public static string ToBase64Image(this byte[] bytes) 
            => $"data:{bytes.FileType()};base64,{bytes?.ToBase64String()}";
    }
}