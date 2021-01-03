using System;

namespace Xpand.Extensions.XAF.Attributes.Custom {
    public class DisplayDateAndTime : Attribute, ICustomAttribute {
        string ICustomAttribute.Name => "DisplayFormat;EditMask";

        string ICustomAttribute.Value => "{0: ddd, dd MMMM yyyy hh:mm:ss tt};ddd, dd MMMM yyyy hh:mm:ss tt";
    }
}