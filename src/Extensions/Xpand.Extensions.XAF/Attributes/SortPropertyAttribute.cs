using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Class)]
    public class SortPropertyAttribute(string name) : Attribute {
        public string Name { get; } = name;
    }
}