using System;

namespace Xpand.Extensions.XAF.Attributes{
    public class MapTypeMembersAttribute(Type source) : Attribute {
        public Type Source{ get; } = source;
    }
}