using System;

namespace Xpand.Extensions.XAF.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class ReadOnlyCollectionAttribute:Attribute {
        

        public ReadOnlyCollectionAttribute() {
            
        }
    }
}