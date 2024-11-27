using System;

namespace Xpand.Extensions.XAF.Attributes.Custom {
    public class DisplayFormatAttribute(string value) : Attribute, ICustomAttribute {
        string ICustomAttribute.Name => "DisplayFormat";

        string ICustomAttribute.Value => value;
    }
}