using System;

namespace Xpand.Extensions.XAF.Attributes{
    public class FireChangedAttribute:Attribute {
        public string[] Properties{ get; }

        public FireChangedAttribute(params string[] properties) {
            Properties = properties;
        }
    }
}