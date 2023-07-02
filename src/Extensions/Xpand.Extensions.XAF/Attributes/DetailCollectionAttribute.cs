using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property)]
    public class DetailCollectionAttribute:Attribute {
        public string MasterCollectionName{ get; }
        public string ChildPropertyName{ get; }

        public DetailCollectionAttribute(string masterCollectionName,string childPropertyName) {
            MasterCollectionName = masterCollectionName;
            ChildPropertyName = childPropertyName;
        }
    }
}