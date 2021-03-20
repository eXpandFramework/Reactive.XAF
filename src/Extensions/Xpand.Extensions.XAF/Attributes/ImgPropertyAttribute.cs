using System;

namespace Xpand.Extensions.XAF.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class ImgPropertyAttribute:Attribute {
        public int Width { get; }

        public ImgPropertyAttribute(int width=0) {
            Width = width;
        }
    }
}