using System;
using DevExpress.ExpressApp.Utils;

namespace Xpand.Extensions.XAF {
    public static class Extensions {
        public static ImageInfo ImageInfo(this Enum @enum) 
            => ImageLoader.Instance.GetEnumValueImageInfo(@enum);
    }
}