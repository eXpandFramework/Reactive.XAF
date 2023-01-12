using System;

namespace Xpand.Extensions.XAF.Attributes{
    public class MapTypeMembersAttribute:Attribute{
        public Type Source{ get; }

        public MapTypeMembersAttribute(Type source) => Source = source;
    }
}