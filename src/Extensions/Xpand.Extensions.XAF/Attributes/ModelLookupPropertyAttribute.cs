using System;

namespace Xpand.Extensions.XAF.Attributes{
    public class ModelLookupPropertyAttribute:Attribute {
        public ModelLookupPropertyAttribute(string property) => Property = property;

        public string Property { get; }
    }
}