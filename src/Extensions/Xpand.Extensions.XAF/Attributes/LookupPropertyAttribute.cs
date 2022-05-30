using System;

namespace Xpand.Extensions.XAF.Attributes{
    public class LookupPropertyAttribute:Attribute {
        public LookupPropertyAttribute(string property) => Property = property;

        public string Property { get; }
    }
}