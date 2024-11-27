using System;

namespace Xpand.Extensions.XAF.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class HyperLinkPropertyEditorAttribute(string name) : Attribute {
        public string Name { get; } = name;
    }
}