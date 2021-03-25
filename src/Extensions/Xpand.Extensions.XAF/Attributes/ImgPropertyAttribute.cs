using System;

namespace Xpand.Extensions.XAF.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class ImgPropertyAttribute:Attribute {
        public ImgPropertyAttribute(int width=20) => Width = width;

        public int DetailViewWidth { get; set; }
        public int Width { get; }
    }
}