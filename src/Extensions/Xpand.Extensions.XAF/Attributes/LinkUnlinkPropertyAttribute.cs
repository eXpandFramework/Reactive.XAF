using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property)]
    public class LinkUnlinkPropertyAttribute:Attribute {
        public string PropertyName{ get; }

        public LinkUnlinkPropertyAttribute(string propertyName) => PropertyName = propertyName;
    }
}