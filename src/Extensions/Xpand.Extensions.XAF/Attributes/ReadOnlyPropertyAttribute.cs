using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReadOnlyPropertyAttribute:Attribute {
        public bool AllowClear { get; }
        public ReadOnlyPropertyAttribute(bool allowClear=false) => AllowClear = allowClear;
    }
}