using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property)]
    public class LinkUnlinkPropertyAttribute(string propertyName) : Attribute {
        public string PropertyName{ get; } = propertyName;
    }
}