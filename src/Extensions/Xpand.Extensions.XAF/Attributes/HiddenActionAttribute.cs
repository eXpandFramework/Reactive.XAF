using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Property,AllowMultiple = true)]
    public class HiddenActionAttribute(params string[] actions) : Attribute {
        public string[] Actions{ get; } = actions;
    }
}