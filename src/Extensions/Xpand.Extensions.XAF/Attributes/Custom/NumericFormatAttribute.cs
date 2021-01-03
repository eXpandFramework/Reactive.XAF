using System;

namespace Xpand.Extensions.XAF.Attributes.Custom {
    public class NumericFormatAttribute : Attribute, ICustomAttribute {
        string ICustomAttribute.Name => "EditMaskAttribute;DisplayFormatAttribute";

        string ICustomAttribute.Value => "f0;#";
    }
}