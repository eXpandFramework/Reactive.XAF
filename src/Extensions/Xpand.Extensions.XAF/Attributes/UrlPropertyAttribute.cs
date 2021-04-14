using System;

namespace Xpand.Extensions.XAF.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class UrlPropertyAttribute:Attribute {
        public bool IsEmail { get; set; }
    }
}