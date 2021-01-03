using System;

namespace Xpand.Extensions.XAF.Attributes.Custom {
    public class DisplayFormatAttribute : Attribute, ICustomAttribute {
        readonly string _value;

        public DisplayFormatAttribute(string value) {
            _value = value;
        }

        string ICustomAttribute.Name => "DisplayFormat";

        string ICustomAttribute.Value => _value;
    }
}