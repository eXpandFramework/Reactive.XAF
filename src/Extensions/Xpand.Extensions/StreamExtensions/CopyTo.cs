namespace Xpand.Extensions.StreamExtensions{
    public static partial class StreamExtensions{
        public static void CopyTo(this System.IO.Stream src, System.IO.Stream dest){
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
                dest.Write(bytes, 0, cnt);
            }
        }

    }
}