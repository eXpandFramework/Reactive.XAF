using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExplicitModificationAttribute(string propertyName) : Attribute {
        public string PropertyName { get; } = propertyName;
    }
}